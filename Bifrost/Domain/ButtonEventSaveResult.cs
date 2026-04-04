namespace cl.MedelCodeFactory.IoT.Bifrost.Domain
{
    public class ButtonEventSaveResult
    {
        public bool Success { get; init; }
        public bool IsDuplicate { get; init; }

        public static ButtonEventSaveResult Inserted()
        {
            return new ButtonEventSaveResult
            {
                Success = true,
                IsDuplicate = false
            };
        }

        public static ButtonEventSaveResult Duplicate()
        {
            return new ButtonEventSaveResult
            {
                Success = true,
                IsDuplicate = true
            };
        }

        public static ButtonEventSaveResult Failed()
        {
            return new ButtonEventSaveResult
            {
                Success = false,
                IsDuplicate = false
            };
        }
    }
}