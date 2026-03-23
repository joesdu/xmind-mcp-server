using System.Text.RegularExpressions;
using XmindMcp.Server.Models;
using XmindMcp.Server.Services;

namespace XmindMcp.Tests;

[TestClass]
public class XmindDocumentTests
{
    [TestMethod]
    public void CreateDocument_ShouldHaveDefaultSheet()
    {
        // Arrange & Act
        var doc = new XmindDocument();

        // Assert
        Assert.IsNotNull(doc.Sheets);
        Assert.AreEqual(0, doc.Sheets.Count);
    }

    [TestMethod]
    public void AddSheet_ShouldAddSheetWithTitle()
    {
        // Arrange
        var doc = new XmindDocument();

        // Act
        var sheet = doc.AddSheet("Test Sheet", "Root Topic");

        // Assert
        Assert.AreEqual(1, doc.Sheets.Count);
        Assert.AreEqual("Test Sheet", sheet.Title);
        Assert.AreEqual("Root Topic", sheet.RootTopic.Title);
    }

    [TestMethod]
    public void GetActiveSheet_ShouldReturnFirstSheet()
    {
        // Arrange
        var doc = new XmindDocument();
        doc.AddSheet("Sheet 1", "Root 1");
        doc.AddSheet("Sheet 2", "Root 2");

        // Act
        var activeSheet = doc.GetActiveSheet();

        // Assert
        Assert.IsNotNull(activeSheet);
        Assert.AreEqual("Sheet 1", activeSheet.Title);
    }

    [TestMethod]
    public void FindSheet_ShouldFindSheetByTitle()
    {
        // Arrange
        var doc = new XmindDocument();
        doc.AddSheet("Sheet 1", "Root 1");
        doc.AddSheet("Sheet 2", "Root 2");

        // Act
        var sheet = doc.FindSheet("Sheet 2");

        // Assert
        Assert.IsNotNull(sheet);
        Assert.AreEqual("Sheet 2", sheet.Title);
    }

    [TestMethod]
    public void RenameSheet_ShouldUpdateSheetTitle()
    {
        var doc = new XmindDocument();
        doc.AddSheet("Old", "Root");
        var result = doc.RenameSheet("Old", "New");
        Assert.IsTrue(result);
        Assert.IsNotNull(doc.FindSheet("New"));
    }

    [TestMethod]
    public void RemoveSheet_ShouldDeleteMatchingSheet()
    {
        var doc = new XmindDocument();
        doc.AddSheet("A", "Root A");
        doc.AddSheet("B", "Root B");
        var result = doc.RemoveSheet("A");
        Assert.IsTrue(result);
        Assert.AreEqual(1, doc.Sheets.Count);
        Assert.IsNull(doc.FindSheet("A"));
    }
}

[TestClass]
public class TopicEditorTests
{
    [TestMethod]
    public void AddChild_ShouldAddTopicToParent()
    {
        // Arrange
        var parent = new Topic { Title = "Parent" };

        // Act
        var child = TopicEditor.AddChild(parent, "Child");

        // Assert
        Assert.IsNotNull(parent.Children);
        Assert.AreEqual(1, parent.Children.Attached!.Count);
        Assert.AreEqual("Child", child.Title);
        Assert.AreEqual(parent, child.Parent);
    }

    [TestMethod]
    public void UpdateTitle_ShouldChangeTitle()
    {
        // Arrange
        var topic = new Topic { Title = "Old Title" };

        // Act
        TopicEditor.UpdateTitle(topic, "New Title");

        // Assert
        Assert.AreEqual("New Title", topic.Title);
    }

    [TestMethod]
    public void AddMarker_ShouldAddMarkerToTopic()
    {
        // Arrange
        var topic = new Topic { Title = "Test" };
        var marker = MarkerConstants.Priority(1);

        // Act
        TopicEditor.AddMarker(topic, marker);

        // Assert
        Assert.IsNotNull(topic.Markers);
        Assert.AreEqual(1, topic.Markers.Count);
        Assert.AreEqual("priority-1", topic.Markers[0].MarkerId);
    }

    [TestMethod]
    public void AddLabel_ShouldAddLabelToTopic()
    {
        // Arrange
        var topic = new Topic { Title = "Test" };

        // Act
        TopicEditor.AddLabel(topic, "Important");

        // Assert
        Assert.IsNotNull(topic.Labels);
        Assert.AreEqual(1, topic.Labels.Count);
        Assert.AreEqual("Important", topic.Labels[0]);
    }

    [TestMethod]
    public void UpdateNotes_ShouldSetNotes()
    {
        // Arrange
        var topic = new Topic { Title = "Test" };

        // Act
        TopicEditor.UpdateNotes(topic, "This is a note");

        // Assert
        Assert.IsNotNull(topic.Notes);
        Assert.AreEqual("This is a note", topic.Notes.Plain?.Content);
    }

    [TestMethod]
    public void RemoveTopic_ShouldRemoveFromParent()
    {
        // Arrange
        var parent = new Topic { Title = "Parent" };
        var child = TopicEditor.AddChild(parent, "Child");

        // Act
        var result = TopicEditor.RemoveTopic(child);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0, parent.Children!.Attached!.Count);
    }

    [TestMethod]
    public void CloneTopic_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = new Topic { Title = "Original" };
        TopicEditor.AddMarker(original, MarkerConstants.Priority(1));
        TopicEditor.AddLabel(original, "Label");
        TopicEditor.UpdateNotes(original, "Notes");
        _ = TopicEditor.AddChild(original, "Child");

        // Act
        var clone = TopicEditor.CloneTopic(original);

        // Assert
        Assert.AreNotEqual(original.Id, clone.Id);
        Assert.AreEqual(original.Title, clone.Title);
        Assert.AreEqual(original.Markers![0].MarkerId, clone.Markers![0].MarkerId);
        Assert.AreEqual(original.Labels![0], clone.Labels![0]);
        Assert.AreEqual(original.Notes!.Plain!.Content, clone.Notes!.Plain!.Content);
        Assert.AreEqual(1, clone.Children!.Attached!.Count);
        Assert.AreEqual("Child", clone.Children.Attached[0].Title);
    }

    [TestMethod]
    public void InsertChild_ShouldRespectRequestedPosition()
    {
        var parent = new Topic { Title = "Parent" };
        TopicEditor.AddChild(parent, "A");
        TopicEditor.AddChild(parent, "C");
        var child = TopicEditor.InsertChild(parent, 1, "B");
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, parent.Children!.Attached!.Select(t => t.Title).ToArray());
        Assert.AreEqual(parent, child.Parent);
    }

    [TestMethod]
    public void MoveTopicToPosition_ShouldMoveTopicUnderNewParentAtIndex()
    {
        var oldParent = new Topic { Title = "Old Parent" };
        var moved = TopicEditor.AddChild(oldParent, "Moved");
        var newParent = new Topic { Title = "New Parent" };
        TopicEditor.AddChild(newParent, "First");
        TopicEditor.MoveTopicToPosition(moved, newParent, 0);
        CollectionAssert.AreEqual(new[] { "Moved", "First" }, newParent.Children!.Attached!.Select(t => t.Title).ToArray());
        Assert.AreEqual(newParent, moved.Parent);
        Assert.AreEqual(0, oldParent.Children!.Attached!.Count);
    }

    [TestMethod]
    public void RemoveMarker_ShouldRemoveExistingMarker()
    {
        var topic = new Topic { Title = "Test" };
        TopicEditor.AddMarker(topic, MarkerConstants.Priority(1));
        var result = TopicEditor.RemoveMarker(topic, MarkerConstants.Priority1);
        Assert.IsTrue(result);
        Assert.AreEqual(0, topic.Markers!.Count);
    }

    [TestMethod]
    public void RemoveLabel_ShouldRemoveExistingLabel()
    {
        var topic = new Topic { Title = "Test" };
        TopicEditor.AddLabel(topic, "Important");
        var result = TopicEditor.RemoveLabel(topic, "Important");
        Assert.IsTrue(result);
        Assert.AreEqual(0, topic.Labels!.Count);
    }

    [TestMethod]
    public void SetLinkAndClearLink_ShouldUpdateHref()
    {
        var topic = new Topic { Title = "Test" };
        TopicEditor.SetLink(topic, "https://example.com");
        Assert.AreEqual("https://example.com", topic.Href);
        TopicEditor.ClearLink(topic);
        Assert.IsNull(topic.Href);
    }
}

[TestClass]
public class TopicSearchEngineTests
{
    private static Sheet CreateTestSheet()
    {
        var sheet = new Sheet
        {
            Title = "Test Sheet",
            RootTopic =
            {
                Title = "Root"
            }
        };
        var child1 = TopicEditor.AddChild(sheet.RootTopic, "C# Programming");
        var child2 = TopicEditor.AddChild(sheet.RootTopic, "Python Programming");
        var child3 = TopicEditor.AddChild(sheet.RootTopic, "Java Development");
        TopicEditor.AddMarker(child1, MarkerConstants.Priority(1));
        TopicEditor.AddLabel(child2, "Important");
        TopicEditor.UpdateNotes(child3, "Learn Java basics");
        return sheet;
    }

    [TestMethod]
    public void FindByTitle_ShouldFindMatchingTopics()
    {
        // Arrange
        var sheet = CreateTestSheet();

        // Act
        var results = TopicSearchEngine.FindByTitle(sheet, "Programming");

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.Any(t => t.Title == "C# Programming"));
        Assert.IsTrue(results.Any(t => t.Title == "Python Programming"));
    }

    [TestMethod]
    public void FindByMarker_ShouldFindTopicsWithMarker()
    {
        // Arrange
        var sheet = CreateTestSheet();

        // Act
        var results = TopicSearchEngine.FindByMarker(sheet, "priority-1");

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("C# Programming", results[0].Title);
    }

    [TestMethod]
    public void FindByLabel_ShouldFindTopicsWithLabel()
    {
        // Arrange
        var sheet = CreateTestSheet();

        // Act
        var results = TopicSearchEngine.FindByLabel(sheet, "Important");

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Python Programming", results[0].Title);
    }

    [TestMethod]
    public void FindByNote_ShouldFindTopicsWithNoteContent()
    {
        // Arrange
        var sheet = CreateTestSheet();

        // Act
        var results = TopicSearchEngine.FindByNote(sheet, "basics");

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Java Development", results[0].Title);
    }

    [TestMethod]
    public void CountTopics_ShouldCountAllTopics()
    {
        // Arrange
        var sheet = CreateTestSheet();

        // Act
        var count = TopicSearchEngine.CountTopics(sheet);

        // Assert
        Assert.AreEqual(4, count); // Root + 3 children
    }

    [TestMethod]
    public void GetLeafNodes_ShouldReturnOnlyLeafTopics()
    {
        // Arrange
        var sheet = CreateTestSheet();

        // Act
        var leaves = TopicSearchEngine.GetLeafNodes(sheet.RootTopic);

        // Assert
        Assert.AreEqual(3, leaves.Count);
        foreach (var leaf in leaves)
        {
            Assert.IsTrue(leaf.Children?.Attached == null || leaf.Children.Attached.Count == 0);
        }
    }

    [TestMethod]
    public void GetAllTopics_ShouldReturnFlatList()
    {
        // Arrange
        var sheet = CreateTestSheet();

        // Act
        var allTopics = TopicSearchEngine.GetAllTopics(sheet);

        // Assert
        Assert.AreEqual(4, allTopics.Count);
    }

    [TestMethod]
    public void GetPath_ShouldReturnFullPath()
    {
        // Arrange
        var sheet = CreateTestSheet();
        var child = sheet.RootTopic.Children!.Attached![0];

        // Act
        var path = child.Path;

        // Assert
        Assert.AreEqual("Root → C# Programming", path);
    }

    [TestMethod]
    public void FindByTitleRegex_WithRegexInstance_ShouldFindMatchingTopics()
    {
        var sheet = CreateTestSheet();
        var regex = new Regex("^C#", RegexOptions.None);
        var results = TopicSearchEngine.FindByTitleRegex(sheet, regex);
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("C# Programming", results[0].Title);
    }
}