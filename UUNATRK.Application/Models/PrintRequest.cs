using UUNATRK.Application.Enums;

namespace UUNATRK.Application.Models
{
    public class PrintRequest
    {
        public Paper? Paper { get; set; }
        public string Width { get; set; } = "210mm";
        public string Height { get; set; } = "297mm";
        public string XPosition { get; set; } = "50mm";
        public string YPosition { get; set; } = "50mm";
        public int Scale { get; set; } = 1;
        public int Rotation { get; set; } = 0;
        public bool InvertX { get; set; } = false;
        public bool InvertY { get; set; } = true;
    }
}
