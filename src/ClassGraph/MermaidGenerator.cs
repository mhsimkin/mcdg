namespace ClassGraph;

public class MermaidGenerator : IDiagramGenerator {
  private static string MDFrame =
      @"```mermaid
classDiagram
    direction {2}

{0}
{1}
```
";

  private static string ClassFrame =
@"class {0} {{
{1}{2}
}}";

  public string Generate(Graph graph, bool highLevelOnly, DiagramDirection diagramDirection) {
    var allClass = new List<string>();
    foreach (var @class in graph.Classes) {
            // fixed an issue where a class with empty name would generate invalid
            // Mermaid syntax and break the entire diagram - just skip any classes with empty/whitespace names
            if (string.IsNullOrWhiteSpace(@class.Name)) continue;
      var classString = GenerateClass(@class, highLevelOnly);
      allClass.Add(classString);
    }

    var allRelation = new List<string>();
    foreach (var relation in graph.Relations) {
            // if either end of the relation has empty/whitespace name, skip it to avoid breaking Mermaid syntax
            if (string.IsNullOrWhiteSpace(relation.From?.Name) ||
            string.IsNullOrWhiteSpace(relation.To?.Name)) continue;

        var relationString = GenerateRelation(relation);
      allRelation.Add(relationString);
    }

    // Join classes with blank line between them
    var classSection = string.Join("\r\n\r\n", allClass);

    // Join relations without blank lines between them
    var relationSection = string.Join("\r\n", allRelation);

    // Add blank line between class section and relation section if both exist
    var sections = classSection;
    if (!string.IsNullOrEmpty(relationSection)) {
      sections += "\r\n\r\n" + relationSection;
    }

    return string.Format(MDFrame, sections, string.Empty, diagramDirection.ToString());
  }

  private string GenerateClass(Class @class, bool highLevelOnly) {
    var lines = new List<string>();

    // Add type annotation (<<interface>>, <<record>>, etc.) as first line inside class block
    var typeAnnotation = GetTypeAnnotation(@class.Kind);
    if (!string.IsNullOrEmpty(typeAnnotation)) {
      lines.Add($"  {typeAnnotation}");
    }

    if (highLevelOnly) {
      return string.Format(ClassFrame, @class.Name, string.Empty, string.Empty);
    }

    // For enums, add enum values instead of properties/methods
    if (@class.Kind == TypeKind.Enum) {
      foreach (var enumValue in @class.EnumValues) {
        lines.Add($"  {enumValue}");
      }
    }
    else {
      // Add properties
      foreach (var property in @class.Properties) {
        lines.Add(GenerateClassProperty(@class.Name, property, @class.Kind));
      }

      // Add methods
      foreach (var method in @class.Methods) {
        lines.Add(GenerateClassMethod(@class.Name, method, @class.Kind));
      }
    }

    // Join all lines without trailing newline
    var content = string.Join("\r\n", lines);

    return string.Format(ClassFrame, @class.Name, content, string.Empty);
  }

  private string GetTypeAnnotation(TypeKind kind) {
    return kind switch {
      TypeKind.Interface => "<<interface>>",
      TypeKind.Record => "<<record>>",
      TypeKind.Struct => "<<struct>>",
      TypeKind.RecordStruct => "<<record struct>>",
      TypeKind.Enum => "<<enumeration>>",
      _ => string.Empty
    };
  }

  private string GenerateClassProperty(string className, Property property, TypeKind typeKind) {
    // Pass the raw Type string (e.g. "List<TimingDose>?")
    var typeString = GetTypeString(property.Type);
    var visibilityNotion = GetVisibilityNotion(property.MemberVisibility);

    // For cleaner Mermaid output, don't prefix with className - just indent
    return $"  {visibilityNotion}{typeString} {property.Name}";
  }

  private string GenerateClassMethod(string className, Method method, TypeKind typeKind) {
    // Pass the raw Type string
    var typeString = GetTypeString(method.Type);
    var visibilityNotion = GetVisibilityNotion(method.MemberVisibility);

    // For cleaner Mermaid output, don't prefix with className - just indent
    return $"  {visibilityNotion}{method.Name}() {typeString}";
  }

  /// <summary>
  /// Converts C# Type string to Mermaid safe string (swapping < > for ~)
  /// </summary>
  private string GetTypeString(string? type) {
    if (string.IsNullOrEmpty(type)) return "void";

    // Mermaid uses ~ for generics, e.g., List~string~
    // It renders "?" correctly as is.
    return type.Replace("<", "~").Replace(">", "~");
  }

  private string GenerateRelation(ClassRelation relation) {
    // For implementation, use the correct Mermaid syntax with "implements" label
    // Interface (To) should be on the left, implementing class (From) on the right
    if (relation.Type == RelationType.Implementation) {
      return $"{relation.To.Name} <|.. {relation.From.Name} : implements";
    }

    var relationNotion = GetRelationNotion(relation.Type);
    return $"{relation.To.Name} {relationNotion} {relation.From.Name}";
  }

  private string GetRelationNotion(RelationType type) {
    switch (type) {
      case RelationType.Inheritance:
        return "<|--";
      case RelationType.Implementation:
        return "..|>"; // Not used directly anymore, handled in GenerateRelation
      case RelationType.Dependency:
        return "<--"; // Defines the arrow direction for dependency
      default:
        return string.Empty;
    }
  }

  private string GetVisibilityNotion(Visibility visibility) {
    switch (visibility) {
      case Visibility.Private: return "-";
      case Visibility.Protected: return "#";
      case Visibility.Public: return "+";
      case Visibility.Internal: return "~";
      default: return string.Empty;
    }
  }
}