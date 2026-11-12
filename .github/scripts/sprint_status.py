"""Posts the daily sprint status to Discord: burndown chart, work in progress and PRs waiting for review.

Reads the GitHub Projects board (Status, Sprint and Estimate fields). The burndown is
calculated from the board itself: on each working day, the remaining hours are the
estimates of the sprint's tickets that were not closed yet at the end of that day.

Environment: WEBHOOK, PROJECT_TOKEN (classic token with read:project), ORG, REPO,
PROJECT_NUMBER. Set DRY_RUN=1 to print the Discord message instead of sending it.
"""

import json
import os
import sys
import urllib.request
from collections import defaultdict
from datetime import date, datetime, timedelta
from zoneinfo import ZoneInfo

TIMEZONE = ZoneInfo("Europe/Amsterdam")

QUERY = """
query($org: String!, $repo: String!, $number: Int!, $after: String) {
  organization(login: $org) {
    projectV2(number: $number) {
      field(name: "Sprint") {
        ... on ProjectV2IterationField {
          configuration {
            iterations { id title startDate duration }
            completedIterations { id title startDate duration }
          }
        }
      }
      items(first: 100, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes {
          content {
            __typename
            ... on Issue { number title url closedAt assignees(first: 5) { nodes { login } } }
          }
          status: fieldValueByName(name: "Status") { ... on ProjectV2ItemFieldSingleSelectValue { name } }
          sprint: fieldValueByName(name: "Sprint") { ... on ProjectV2ItemFieldIterationValue { iterationId } }
          estimate: fieldValueByName(name: "Estimate") { ... on ProjectV2ItemFieldNumberValue { number } }
        }
      }
    }
  }
  repository(owner: $org, name: $repo) {
    pullRequests(states: OPEN, first: 50, orderBy: { field: CREATED_AT, direction: ASC }) {
      nodes { number title url isDraft createdAt reviewDecision author { login } }
    }
  }
}
"""


def post_json(url, payload, headers=None):
    request = urllib.request.Request(
        url,
        data=json.dumps(payload).encode(),
        headers={"Content-Type": "application/json", "User-Agent": "vakantiepark-sprint-status", **(headers or {})},
    )
    with urllib.request.urlopen(request, timeout=30) as response:
        body = response.read()
        return json.loads(body) if body else None


def fetch_board(token, org, repo, number):
    items, after, data = [], None, None
    while True:
        result = post_json(
            "https://api.github.com/graphql",
            {"query": QUERY, "variables": {"org": org, "repo": repo, "number": number, "after": after}},
            {"Authorization": f"Bearer {token}"},
        )
        if result.get("errors"):
            sys.exit(f"GitHub API error: {result['errors']}")
        data = result["data"]
        page = data["organization"]["projectV2"]["items"]
        items += page["nodes"]
        if not page["pageInfo"]["hasNextPage"]:
            break
        after = page["pageInfo"]["endCursor"]
    configuration = data["organization"]["projectV2"]["field"]["configuration"]
    iterations = configuration["iterations"] + configuration["completedIterations"]
    pull_requests = data["repository"]["pullRequests"]["nodes"]
    return items, iterations, pull_requests


def pick_sprint(iterations, today):
    """The sprint running today, otherwise the next one that has not started yet."""
    for iteration in iterations:
        start = date.fromisoformat(iteration["startDate"])
        if start <= today < start + timedelta(days=iteration["duration"]):
            return iteration
    upcoming = [i for i in iterations if date.fromisoformat(i["startDate"]) > today]
    return min(upcoming, key=lambda i: i["startDate"]) if upcoming else None


def closed_on(item):
    closed_at = item["content"].get("closedAt")
    if not closed_at:
        return None
    return datetime.fromisoformat(closed_at.replace("Z", "+00:00")).astimezone(TIMEZONE).date()


def create_chart(title, labels, ideal, actual):
    chart = {
        "type": "line",
        "data": {
            "labels": labels,
            "datasets": [
                {"label": "Ideaal", "data": ideal, "borderColor": "#95a5a6", "borderDash": [6, 4],
                 "fill": False, "pointRadius": 0},
                {"label": "Resterend", "data": actual, "borderColor": "#5865F2",
                 "backgroundColor": "rgba(88,101,242,0.15)", "fill": True, "lineTension": 0},
            ],
        },
        "options": {
            "title": {"display": True, "text": f"Burndown {title}"},
            "scales": {"yAxes": [{"ticks": {"beginAtZero": True}, "scaleLabel": {"display": True, "labelString": "Uren"}}]},
        },
    }
    result = post_json("https://quickchart.io/chart/create",
                       {"chart": chart, "width": 800, "height": 400, "backgroundColor": "white", "format": "png"})
    return result["url"]


def build_message(items, iterations, pull_requests, today):
    sprint = pick_sprint(iterations, today)
    if sprint is None:
        return {"username": "Sprint Status", "content": "Er staat geen huidige of komende sprint op het bord."}

    start = date.fromisoformat(sprint["startDate"])
    days = [start + timedelta(days=i) for i in range(sprint["duration"]) if (start + timedelta(days=i)).weekday() < 5]
    tickets = [i for i in items
               if i["content"] and i["content"]["__typename"] == "Issue"
               and i["sprint"] and i["sprint"]["iterationId"] == sprint["id"]]
    hours = {id(t): (t["estimate"] or {}).get("number") or 0 for t in tickets}
    total = sum(hours.values())

    def remaining_on(day):
        return sum(hours[id(t)] for t in tickets if not (closed_on(t) and closed_on(t) <= day))

    elapsed = [d for d in days if d <= today]
    actual = [remaining_on(d) for d in elapsed]
    steps = max(len(days) - 1, 1)
    ideal = [round(total - total * i / steps, 1) for i in range(len(days))]
    remaining = remaining_on(today)
    done = sum(1 for t in tickets if closed_on(t) and closed_on(t) <= today)

    if today < start:
        headline = f"📅 {sprint['title']} begint op {start.strftime('%d-%m')}"
        summary = f"**{total:g} uur** gepland in {len(tickets)} tickets."
    else:
        day_number = len(elapsed)
        ideal_now = ideal[day_number - 1]
        difference = remaining - ideal_now
        pace = "🟢 op of voor schema" if difference <= 0 else f"🟠 {difference:g} uur achter op schema"
        headline = f"📉 {sprint['title']} · dag {day_number} van {len(days)}"
        summary = (f"**{remaining:g} van {total:g} uur** resterend · {done}/{len(tickets)} tickets afgerond\n"
                   f"Ideaal vandaag: {ideal_now:g} uur · {pace}")

    in_progress = defaultdict(list)
    for t in tickets:
        if (t["status"] or {}).get("name") == "In progress":
            owners = [a["login"] for a in t["content"]["assignees"]["nodes"]] or ["niemand toegewezen"]
            for owner in owners:
                in_progress[owner].append(t["content"]["title"].split(" - ")[0])
    progress_text = "\n".join(f"**{owner}**: {', '.join(sorted(stories))}"
                              for owner, stories in sorted(in_progress.items())) or "Niets in progress."

    waiting = [pr for pr in pull_requests if not pr["isDraft"] and pr["reviewDecision"] != "APPROVED"]
    review_lines = []
    for pr in waiting:
        age = (today - datetime.fromisoformat(pr["createdAt"].replace("Z", "+00:00")).astimezone(TIMEZONE).date()).days
        author = (pr["author"] or {}).get("login", "?")
        review_lines.append(f"[#{pr['number']} {pr['title']}]({pr['url']}) · {author} · {age} d")
    review_text = "\n".join(review_lines) or "Geen PR's die op een review wachten."

    embed = {
        "title": headline,
        "description": summary,
        "color": 5793266,
        "fields": [
            {"name": "🛠️ Onderhanden", "value": progress_text[:1024]},
            {"name": "👀 Wacht op review", "value": review_text[:1024]},
        ],
        "timestamp": datetime.now(TIMEZONE).isoformat(),
    }
    labels = [d.strftime("%d-%m") for d in days]
    try:
        embed["image"] = {"url": create_chart(sprint["title"], labels, ideal, actual)}
    except OSError as error:
        # The status is still useful without the picture; the next run tries again.
        print(f"Chart could not be created ({error}); posting without it.")
    return {"username": "Sprint Status", "embeds": [embed]}


def main():
    token = os.environ.get("PROJECT_TOKEN")
    webhook = os.environ.get("WEBHOOK")
    dry_run = os.environ.get("DRY_RUN") == "1"
    if not token or (not webhook and not dry_run):
        print("Secrets PROJECT_TOKEN and DISCORD_WEBHOOK_SPRINT are required; skipping.")
        return

    items, iterations, pull_requests = fetch_board(
        token, os.environ["ORG"], os.environ["REPO"], int(os.environ.get("PROJECT_NUMBER", "3")))
    message = build_message(items, iterations, pull_requests, datetime.now(TIMEZONE).date())

    if dry_run:
        print(json.dumps(message, indent=2, ensure_ascii=False))
    else:
        post_json(webhook, message)
        print("Posted sprint status to Discord.")


if __name__ == "__main__":
    main()
