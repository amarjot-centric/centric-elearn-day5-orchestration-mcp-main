namespace LearnHub.Tests;

public class BookmarkTests
{
    [Fact]
    public void AddBookmark_Works_ThenRejectsDuplicateIgnoringCase()
    {
        using var env = new TestEnv();
        var first = env.Bookmarks.AddBookmark("Asha", 1);
        var second = env.Bookmarks.AddBookmark("ASHA", 1);

        Assert.NotNull(first.Bookmark);
        Assert.Null(second.Bookmark);
        Assert.Equal("Already bookmarked.", second.Error);
    }

    [Fact]
    public void AddBookmark_RejectsEmptyNameAndUnknownCourse()
    {
        using var env = new TestEnv();
        Assert.Equal("studentName is required.", env.Bookmarks.AddBookmark(" ", 1).Error);
        Assert.Equal("Course not found.", env.Bookmarks.AddBookmark("Asha", 999).Error);
    }

    [Fact]
    public void GetBookmarks_ReturnsOnlyThatStudentsBookmarks()
    {
        using var env = new TestEnv();
        env.Bookmarks.AddBookmark("Asha", 1);
        env.Bookmarks.AddBookmark("Asha", 2);
        env.Bookmarks.AddBookmark("Ben", 3);

        Assert.Equal(new[] { 1, 2 }, env.Bookmarks.GetBookmarks("asha").Select(b => b.CourseId));
        Assert.Single(env.Bookmarks.GetBookmarks("Ben"));
        Assert.Empty(env.Bookmarks.GetBookmarks("Nobody"));
    }

    [Fact]
    public void RemoveBookmark_RemovesOnce_ThenReturnsFalse()
    {
        using var env = new TestEnv();
        env.Bookmarks.AddBookmark("Asha", 1);

        Assert.True(env.Bookmarks.RemoveBookmark("ASHA", 1));
        Assert.False(env.Bookmarks.RemoveBookmark("Asha", 1));
        Assert.Empty(env.Bookmarks.GetBookmarks("Asha"));
    }

    [Fact]
    public void BookmarkIds_AreNeverReused()
    {
        using var env = new TestEnv();
        var a = env.Bookmarks.AddBookmark("Asha", 1).Bookmark!;
        env.Bookmarks.RemoveBookmark("Asha", 1);
        var b = env.Bookmarks.AddBookmark("Asha", 1).Bookmark!;

        Assert.NotEqual(a.Id, b.Id);
    }
}
