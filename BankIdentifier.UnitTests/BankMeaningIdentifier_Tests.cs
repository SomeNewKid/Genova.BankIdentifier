// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.BankIdentifier.UnitTests;

public sealed class BankMeaningIdentifier_Tests
{
    [Fact]
    public void Constructor_should_instantiate_successfully()
    {
        // act
        using var identifier = new BankMeaningIdentifier();

        // assert
        identifier.Should().NotBeNull();
    }

    [Theory]
    [InlineData("He sat on the bank of the river watching the water flow.")]
    [InlineData("The dog splashed off the bank and into the stream.")]
    [InlineData("Mist rose gently above the bank of the river.")]
    [InlineData("He sat on the BANK of the river watching the water flow.")]
    [InlineData("The dog splashed off the BANK and into the stream.")]
    [InlineData("Mist rose gently above the BANK of the river.")]
    public void GetMeaning_should_return_River_for_river_sentences(string sentence)
    {
        using var identifier = new BankMeaningIdentifier();

        BankMeaning meaning = identifier.GetMeaning(sentence);

        meaning.Should().Be(BankMeaning.River,
            "the sentence clearly refers to a river bank");
    }

    [Theory]
    [InlineData("The bank approved my loan application yesterday.")]
    [InlineData("I deposited money at the bank this morning.")]
    [InlineData("The bank notified customers about a security update.")]
    [InlineData("The BANK approved my loan application yesterday.")]
    [InlineData("I deposited money at the BANK this morning.")]
    [InlineData("The BANK notified customers about a security update.")]
    public void GetMeaning_should_return_Financial_for_financial_sentences(string sentence)
    {
        using var identifier = new BankMeaningIdentifier();

        BankMeaning meaning = identifier.GetMeaning(sentence);

        meaning.Should().Be(BankMeaning.Financial,
            "the sentence clearly refers to a financial bank");
    }

    [Theory]
    [InlineData("The plane began to bank sharply to the left.")]
    [InlineData("He stored the seeds in a genetic bank for future study.")]
    [InlineData("The plane began to BANK sharply to the left.")]
    [InlineData("He stored the seeds in a genetic BANK for future study.")]
    public void GetMeaning_should_return_Other_for_non_river_non_financial_sentences(string sentence)
    {
        using var identifier = new BankMeaningIdentifier();

        BankMeaning meaning = identifier.GetMeaning(sentence);

        meaning.Should().Be(BankMeaning.Other,
            "the sentence uses a meaning of 'bank' unrelated to rivers or finance");
    }

    [Theory]
    [InlineData("This sentence does not have the expected word.")]
    [InlineData("This project has taken a bit too long.")]
    public void GetMeaning_should_return_None_for_sentences_without_the_word_bank(string sentence)
    {
        using var identifier = new BankMeaningIdentifier();

        BankMeaning meaning = identifier.GetMeaning(sentence);

        meaning.Should().Be(BankMeaning.None,
            "the sentence does not use the word 'bank'");
    }
}
