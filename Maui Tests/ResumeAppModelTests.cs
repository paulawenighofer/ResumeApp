using ResumeApp.Models;
using System.Globalization;

namespace Test.Unit;

public class ResumeAppModelTests
{
    [Theory]
    [InlineData("Beginner", 1)]
    [InlineData("Intermediate", 2)]
    [InlineData("Advanced", 3)]
    [InlineData("Expert", 4)]
    [InlineData("Unknown", 0)]
    public void SkillEntry_ProficiencyScore_MatchesExpectedValue(string level, int expectedScore)
    {
        var skill = new SkillEntry { ProficiencyLevel = level };

        Assert.Equal(expectedScore, skill.ProficiencyScore);
    }

    [Fact]
    public void SkillEntry_DefaultValues_AreUsable()
    {
        var skill = new SkillEntry();

        Assert.False(string.IsNullOrWhiteSpace(skill.Id));
        Assert.Equal(string.Empty, skill.Name);
        Assert.Equal("Intermediate", skill.ProficiencyLevel);
        Assert.Equal("Programming Language", skill.Category);
    }

    [Fact]
    public void ExperienceEntry_TechList_TrimsAndRemovesEmptyValues()
    {
        var experience = new ExperienceEntry
        {
            Technologies = " C#, .NET , , Azure,  Git "
        };

        Assert.Equal(["C#", ".NET", "Azure", "Git"], experience.TechList);
    }

    [Fact]
    public void ExperienceEntry_DurationText_UsesMonthAndYearFormatting()
    {
        var experience = new ExperienceEntry
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 2, 1)
        };

        Assert.Equal("1 month", experience.DurationText);
    }

    [Fact]
    public void ExperienceEntry_CurrentJob_UsesNowForDuration()
    {
        var experience = new ExperienceEntry
        {
            StartDate = DateTime.Now.AddDays(-65),
            EndDate = DateTime.Now.AddDays(-1),
            IsCurrentJob = true
        };

        Assert.Contains("month", experience.DurationText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EducationEntry_DurationText_ReturnsYears()
    {
        var education = new EducationEntry
        {
            StartDate = new DateTime(2023, 1, 1),
            EndDate = new DateTime(2024, 1, 2)
        };

        Assert.Equal("1 year", education.DurationText);
    }

    [Fact]
    public void ProjectEntry_TechList_TrimsAndRemovesEmptyValues()
    {
        var project = new ProjectEntry
        {
            Technologies = "C#, MAUI, , SQLite,  REST APIs"
        };

        Assert.Equal(["C#", "MAUI", "SQLite", "REST APIs"], project.TechList);
    }

    [Fact]
    public void ProjectEntry_DurationText_ReturnsMonths()
    {
        var project = new ProjectEntry
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 2, 1)
        };

        Assert.Equal("1 month", project.DurationText);
    }

    [Fact]
    public void CertificationEntry_ValidityText_UsesConfiguredDates()
    {
        var certification = new CertificationEntry
        {
            IssueDate = new DateTime(2024, 1, 15),
            ExpirationDate = new DateTime(2025, 6, 10)
        };

        var expected = $"Issued {certification.IssueDate.ToString("MMM yyyy", CultureInfo.CurrentCulture)} • Expires {certification.ExpirationDate.ToString("MMM yyyy", CultureInfo.CurrentCulture)}";

        Assert.Equal(expected, certification.ValidityText);
    }

    [Fact]
    public void ResumeEntry_DefaultCollections_AreInitialized()
    {
        var resume = new ResumeEntry();

        Assert.NotNull(resume.BulletPoints);
        Assert.Empty(resume.BulletPoints);
        Assert.False(resume.IsGenerated);
    }

    [Fact]
    public void ResumePreviewSection_DefaultItems_AreInitialized()
    {
        var section = new ResumePreviewSection();

        Assert.Equal(string.Empty, section.Title);
        Assert.NotNull(section.Items);
        Assert.Empty(section.Items);
    }
}
