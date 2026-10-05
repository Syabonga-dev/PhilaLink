using PersonalProject.Utilities;
using Xunit;

namespace PersonalProject.Tests.Regression
{
    public class SouthAfricanIdNumberTests
    {
        [Fact]
        public void
            DateOfBirthIsDerivedFromFirstSixDigits()
        {
            var dateOfBirth =
                SouthAfricanIdNumber
                    .GetDateOfBirth(
                        "9001015000000"
                    );

            Assert.Equal(
                new DateOnly(
                    1990,
                    1,
                    1
                ),
                dateOfBirth
            );
        }

        [Fact]
        public void
            UpdatingDateOfBirthPreservesRemainingDigits()
        {
            var current =
                "9001015000000";

            var updated =
                SouthAfricanIdNumber
                    .WithDateOfBirth(
                        current,
                        new DateOnly(
                            1985,
                            12,
                            25
                        )
                    );

            Assert.Equal(
                "8512255000000",
                updated
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("123")]
        [InlineData("ABCDEFGHIJKLM")]
        public void
            InvalidIdFormatIsRejected(
                string idNumber
            )
        {
            Assert.Throws<
                InvalidOperationException
            >(
                () =>
                    SouthAfricanIdNumber
                        .Normalize(
                            idNumber
                        )
            );
        }

        [Fact]
        public void
            ImpossibleDateIsRejected()
        {
            Assert.Throws<
                InvalidOperationException
            >(
                () =>
                    SouthAfricanIdNumber
                        .GetDateOfBirth(
                            "9013325000000"
                        )
            );
        }
    }
}
