public class QuestDTO {
    public string Name { get; set; }
    public string Description { get; set; }
}

public class Quest : IValidatedGameAsset {
    string Name { get; set; }
    string Description { get; set; }

    public bool Validate() {
        return Name.Length > 0 && Description.Length > 0;
    }

    public string Stringify() {
        return $"Quest: {Name}\nDescription: {Description}";
    }
}
