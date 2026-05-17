namespace ClassGraph;

public interface IDiagramGenerator
{
    string Generate(Graph graph, bool highLevelOnly, DiagramDirection diagramDirection);
}