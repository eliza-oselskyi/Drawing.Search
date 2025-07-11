namespace Drawing.Search.Common.Interfaces;

/// <summary>
///     Declares methods for observers of search classes.
/// </summary>
public interface IObserver
{
    void OnMatchFound(object obj);
}

// TODO: Remove unnecessary comment
/*
public class Driver
{
    public static void Drive()
    {
        var data = new List<string>
        {
            "hello world",
            "this is a hello",
            "goodbye",
            "TESTING"
        };

        var dh = new DrawingHandler();
        var model = new Model();
        var allObjects = dh.GetModelObjectIdentifiers(dh.GetActiveDrawing());
        var objList = model.FetchModelObjects(allObjects, false);
        var selector = dh.GetDrawingObjectSelector();

        var objs = dh.GetActiveDrawing().GetSheet().GetAllObjects();
        var dolist = new List<Mark>();

        foreach (DrawingObject obj in objs)
        {
            if (obj is Mark mark)
            {
                dolist.Add(mark);
            }
        }

        var searcher = new ObservableSearch<Mark>([new RegexMatchStrategy<Mark>()], new MarkExtractor());

        var searchObserver = new SearchResultObserver();
        searcher.Subscribe(searchObserver);

        var results = searcher.Search(dolist, new SearchQuery("ltmesa"));

        var texts = results.ToList();
        foreach (var res in texts)
        {
            Console.WriteLine($"Found item: {res.Attributes.Content.GetUnformattedString()}");
        }

        Console.WriteLine($"Number of results: {searchObserver.Matches}");

        var someList = texts.Select(DrawingObject (text) => text).ToList();

        TeklaWrapper.DrawingObjectListToSelection(someList, dh.GetActiveDrawing());
    }

}
*/