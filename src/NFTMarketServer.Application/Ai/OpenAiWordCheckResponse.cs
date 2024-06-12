using System.Collections.Generic;

namespace NFTMarketServer.Ai;

public class OpenAiWordCheckResponse
{
    public List<AIResult> Results { get; set; }
}

public class AIResult
{
    public bool Flagged  { get; set; }
    public Category  Categories { get; set; }
}

public class Category
{
    public bool Sexual { get; set; }
    public bool Hate { get; set; }
    public bool Harassment { get; set; }
    public bool SelfHarm { get; set; }
    public bool SexualMinors { get; set; }
    public bool HateThreatening { get; set; }
    public bool ViolenceGraphic { get; set; }
    public bool SelfHarmIntent { get; set; }
    public bool SelfHarmInstructions { get; set; }
    public bool HarassmentThreatening { get; set; }
    public bool Violence { get; set; }
}

