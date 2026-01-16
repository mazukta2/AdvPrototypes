namespace Postica.BindingSystem
{
    /// <summary>
    /// The bind mode defines the direction of a bind operation relative to the bind source. <br/>
    /// That is, a read operation will read data from the bind source 
    /// and a write operation will write the data to the source.
    /// </summary>
    public enum BindMode
    {
        Read,
        Write,
        ReadWrite
    }

    public static class BindModeExtensions
    {
        /// <summary>
        /// Returns a Boolean indicating whether the mode is set to BindMode.Read or BindMode.ReadWrite.
        /// </summary>
        /// <param name="mode">The BindMode value to check.</param>
        /// <returns>True if the mode is set to BindMode.Read or BindMode.ReadWrite, False otherwise.</returns>
        public static bool CanRead(this BindMode mode) => mode == BindMode.Read || mode == BindMode.ReadWrite;
        /// <summary>
        /// Returns a Boolean indicating whether the mode is set to BindMode.Write or BindMode.ReadWrite.
        /// </summary>
        /// <param name="mode">The BindMode value to check.</param>
        /// <returns>True if the mode is set to BindMode.Write or BindMode.ReadWrite, False otherwise.</returns>
        public static bool CanWrite(this BindMode mode) => mode == BindMode.Write || mode == BindMode.ReadWrite;

        /// <summary>
        /// Returns a string representation of the mode in the form of a short string (e.g. "R", "W", "RW").
        /// </summary>
        /// <param name="mode">The BindMode value to convert.</param>
        /// <returns>A string representation of the mode in the form of a short string.</returns>
        public static string ToShortName(this BindMode mode)
        {
            switch (mode)
            {
                case BindMode.Read: return "R";
                case BindMode.Write: return "W";
                case BindMode.ReadWrite: return "RW";
                default: return "-";
            }
        }

        /// <summary>
        /// Returns the next mode in the sequence of BindMode.Read, BindMode.Write, and BindMode.ReadWrite.
        /// </summary>
        /// <param name="mode">The BindMode value to get the next mode for.</param>
        /// <returns>The next mode in the sequence of BindMode.Read, BindMode.Write, and BindMode.ReadWrite.</returns>
        public static BindMode NextMode(this BindMode mode)
        {
            switch (mode)
            {
                case BindMode.Read: return BindMode.Write;
                case BindMode.Write: return BindMode.ReadWrite;
                case BindMode.ReadWrite: return BindMode.Read;
                    default: return BindMode.ReadWrite;
            }
        }

        /// <summary>
        /// Returns a Boolean indicating whether the current mode is compatible with <paramref name="other"/> mode. 
        /// Being compatible means that a mode is a subset of or equals to the other.
        /// </summary>
        /// <param name="mode">The BindMode value to check compatibility for.</param>
        /// <param name="other">The BindMode value to compare to.</param>
        /// <returns>True if the current mode is compatible with the other, False otherwise.</returns>
        public static bool IsCompatibleWith(this BindMode mode, BindMode other)
        {
            return mode == other || mode == BindMode.ReadWrite || other == BindMode.ReadWrite;
        }
        
        /// <summary>
        /// Returns a Boolean indicating whether the current mode is compatible with passed parameters. 
        /// Being compatible means that a mode is a subset of or equals to the parameters.
        /// </summary>
        /// <param name="mode">The BindMode value to check compatibility for.</param>
        /// <param name="canRead">Whether the mode can read</param>
        /// <param name="canWrite">Whether the mode can write</param>
        /// <returns>True if the current mode is compatible with the other, False otherwise.</returns>
        public static bool IsCompatibleWith(this BindMode mode, bool canRead, bool canWrite)
        {
            return (mode == BindMode.Read && canRead) 
                   || (mode == BindMode.Write && canWrite) 
                   || (mode == BindMode.ReadWrite && canWrite && canRead);
        }
    }
}
