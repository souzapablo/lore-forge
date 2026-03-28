using LoreForge.Core.Entities;

namespace LoreForge.UnitTests.Entities;

public class WorkNotesTests
{
    [Fact(DisplayName = "Uses only the title when no notes fields are set")]
    public void Should_ReturnTitleOnly_When_NoNotesAreSet()
    {
        var notes = new WorkNotes();

        var text = notes.ToEmbeddingText("Only Title");

        Assert.Equal("Only Title", text);
    }

    [Fact(DisplayName = "Includes non-null notes fields in the embedding text")]
    public void Should_IncludeNotesFields_When_TheyAreNotNull()
    {
        var notes = new WorkNotes
        {
            Worldbuilding = "The Shattered Realm",
            Themes = "Sacrifice"
        };

        var text = notes.ToEmbeddingText("Test");

        Assert.Contains("Worldbuilding: The Shattered Realm", text);
        Assert.Contains("Themes: Sacrifice", text);
    }

    [Fact(DisplayName = "Excludes null notes fields from the embedding text")]
    public void Should_ExcludeNotesFields_When_TheyAreNull()
    {
        var notes = new WorkNotes { Magic = "Some magic" };

        var text = notes.ToEmbeddingText("Test");

        Assert.DoesNotContain("Worldbuilding:", text);
        Assert.DoesNotContain("Themes:", text);
        Assert.Contains("Magic: Some magic", text);
    }
}
