namespace shared.Event;

public class DocumentUploadEvent
{
    public int DocumentID {get; set;}
    public string Filename {get; set;} = string.Empty;
    public string Fileurl {get; set;} = string.Empty;
}