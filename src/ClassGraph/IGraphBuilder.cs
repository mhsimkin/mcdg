namespace ClassGraph;

public interface IGraphBuilder
{
    Graph Build(IEnumerable<string> files, IEnumerable<string> nsList, IEnumerable<string> typenameList, bool inheretanceOnly);
}