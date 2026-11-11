using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vakantiepark.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Vakantiepark.Infrastructure.Persistence.Configurations
{
    public static class BookingConfiguration
    {
        private const int BookingNumberMaxLength = 12;
        private const int NameMaxLength = 100;
        private const int EmailMaxLength = 254;
        private const int PhoneNumberMaxLength = 20;
        private const int StatusMaxLength = 20;
        private const int MoneyPrecision = 10;
        private const int MoneyScale = 2;

        public static void Configure(this EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BookingNumber)
                .IsRequired()
                .HasMaxLength(BookingNumberMaxLength);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(NameMaxLength);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(EmailMaxLength);

            builder.Property(x => x.PhoneNumber)
                .IsRequired()
                .HasMaxLength(PhoneNumberMaxLength);

            builder.Property(x => x.AccommodationPrice).HasPrecision(MoneyPrecision, MoneyScale);
            builder.Property(x => x.CleaningFee).HasPrecision(MoneyPrecision, MoneyScale);
            builder.Property(x => x.TouristTax).HasPrecision(MoneyPrecision, MoneyScale);
            builder.Property(x => x.TotalPrice).HasPrecision(MoneyPrecision, MoneyScale);

            // Stored as text so the database stays readable and reordering the enum can't corrupt data.
            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(StatusMaxLength);

            // Guests look up their booking by this number, so it must be unique.
            builder.HasIndex(x => x.BookingNumber).IsUnique();

            // Supports the availability check: overlapping bookings for one unit.
            builder.HasIndex(x => new { x.AccommodationUnitId, x.ArrivalDate, x.DepartureDate });

            builder.ToTable(table =>
            {
                table.HasCheckConstraint("ck_bookings_departure_after_arrival", "departure_date > arrival_date");
                table.HasCheckConstraint("ck_bookings_number_of_guests_positive", "number_of_guests > 0");
            });
        }
    }
}
