namespace UUNATRK.Application.Enums
{
    public enum Paper
    {
        A3,
        A4,
        A5,
        A6,
        B4,
        B5,
        Letter,
        Legal,
        Tabloid,
        Envelope_10,
        Envelope_9,
        Envelope_C5,
        Envelope_C6,
        Envelope_DL,
        Card_4x6,
        Card_5x7,
        Custom
    }

    public static class PaperSizes
    {
        /// <summary>
        /// Returns (widthMm, heightMm) for the given paper type.
        /// </summary>
        public static (double Width, double Height) GetSizeMm(Paper paper) => paper switch
        {
            Paper.A3 => (297, 420),
            Paper.A4 => (210, 297),
            Paper.A5 => (148, 210),
            Paper.A6 => (105, 148),
            Paper.B4 => (250, 353),
            Paper.B5 => (176, 250),
            Paper.Letter => (216, 279),
            Paper.Legal => (216, 356),
            Paper.Tabloid => (279, 432),
            Paper.Envelope_10 => (105, 241),
            Paper.Envelope_9 => (98, 225),
            Paper.Envelope_C5 => (162, 229),
            Paper.Envelope_C6 => (114, 162),
            Paper.Envelope_DL => (110, 220),
            Paper.Card_4x6 => (102, 152),
            Paper.Card_5x7 => (127, 178),
            _ => (210, 297) // default to A4
        };
    }
}
