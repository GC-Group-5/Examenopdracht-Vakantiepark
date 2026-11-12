using Vakantiepark.Application.Validations.Seasons;
using Vakantiepark.Domain.Entities;
using Xunit;

namespace Vakantiepark.UnitTests
{
    public class SeasonValidationTests
    {
        [Fact]
        public void ValidateNoOverlap_SeasonFullyInsideExisting_ThrowsArgumentException()
        {
            Season existing = TestData.CreateSeason(x =>
            {
                x.Name = "Middenseizoen 2026";
                x.StartDate = new DateOnly(2026, 4, 1);
                x.EndDate = new DateOnly(2026, 6, 30);
            });
            Season candidate = TestData.CreateSeason(x =>
            {
                x.Name = "Extra seizoen 2026";
                x.StartDate = new DateOnly(2026, 5, 1);
                x.EndDate = new DateOnly(2026, 5, 31);
            });

            ArgumentException exception = Assert.Throws<ArgumentException>(() => candidate.ValidateNoOverlap([existing]));

            Assert.Equal("Dit seizoen overlapt een bestaand seizoen.", exception.Message);
        }

        [Fact]
        public void ValidateNoOverlap_SameRangeAsExisting_ThrowsArgumentException()
        {
            Season existing = TestData.CreateSeason(x => x.Name = "Hoogseizoen 2026");
            Season candidate = TestData.CreateSeason(x =>
            {
                x.Name = "Feestweek 2026";
                x.StartDate = new DateOnly(2026, 7, 1);
                x.EndDate = new DateOnly(2026, 8, 31);
            });

            Assert.Throws<ArgumentException>(() => candidate.ValidateNoOverlap([existing]));
        }

        [Fact]
        public void ValidateNoOverlap_PartialOverlapAtTheStart_ThrowsArgumentException()
        {
            Season existing = TestData.CreateSeason(x =>
            {
                x.Name = "Najaarseizoen 2026";
                x.StartDate = new DateOnly(2026, 9, 1);
                x.EndDate = new DateOnly(2026, 12, 31);
            });
            Season candidate = TestData.CreateSeason(x =>
            {
                x.Name = "Herfstweken 2026";
                x.StartDate = new DateOnly(2026, 8, 15);
                x.EndDate = new DateOnly(2026, 9, 15);
            });

            Assert.Throws<ArgumentException>(() => candidate.ValidateNoOverlap([existing]));
        }

        [Fact]
        public void ValidateNoOverlap_AdjacentSeason_DoesNotThrow()
        {
            Season existing = TestData.CreateSeason(x =>
            {
                x.Name = "Middenseizoen 2026";
                x.StartDate = new DateOnly(2026, 4, 1);
                x.EndDate = new DateOnly(2026, 6, 30);
            });
            Season candidate = TestData.CreateSeason(x =>
            {
                x.Name = "Hoogseizoen 2026";
                x.StartDate = new DateOnly(2026, 7, 1);
                x.EndDate = new DateOnly(2026, 8, 31);
            });

            candidate.ValidateNoOverlap([existing]);
        }

        [Fact]
        public void ValidateNoOverlap_SeasonInOtherYear_DoesNotThrow()
        {
            Season existing = TestData.CreateSeason(x =>
            {
                x.Name = "Hoogseizoen 2026";
                x.StartDate = new DateOnly(2026, 7, 1);
                x.EndDate = new DateOnly(2026, 8, 31);
            });
            Season candidate = TestData.CreateSeason(x =>
            {
                x.Name = "Hoogseizoen 2027";
                x.StartDate = new DateOnly(2027, 7, 1);
                x.EndDate = new DateOnly(2027, 8, 31);
            });

            candidate.ValidateNoOverlap([existing]);
        }

        [Fact]
        public void ValidateNoOverlap_SameSeasonBeingEdited_DoesNotThrowAgainstItself()
        {
            Season season = TestData.CreateSeason(x => x.Name = "Hoogseizoen 2026");

            season.ValidateNoOverlap([season]);
        }

        [Fact]
        public void ValidateNoOverlap_EndBeforeStart_ThrowsArgumentException()
        {
            Season candidate = TestData.CreateSeason(x =>
            {
                x.StartDate = new DateOnly(2026, 9, 15);
                x.EndDate = new DateOnly(2026, 9, 1);
            });

            ArgumentException exception = Assert.Throws<ArgumentException>(() => candidate.ValidateNoOverlap([]));

            Assert.Equal("De begindatum van een seizoen moet vóór de einddatum liggen.", exception.Message);
        }
    }
}
