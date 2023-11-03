namespace Morph.Server.Sdk.Model.SharedMemory
{
    /// <summary>
    /// Represents a value in shared memory service
    /// </summary>
    public abstract class SharedMemoryValue
    {
        /// <summary>
        /// Represents an error
        /// </summary>
        public class Error : SharedMemoryValue
        {
            /// <summary>
            /// Error message
            /// </summary>
            public string Message { get; set; } = string.Empty;
            
            public override object Contents => Message;
        }

        /// <summary>
        /// Represents a boolean value
        /// </summary>
        public class Boolean : SharedMemoryValue
        {
            /// <summary>
            /// Boolean value
            /// </summary>
            public bool Value { get; set; }
            
            public override object Contents => Value;
        }

        /// <summary>
        /// Represents a text value
        /// </summary>
        public class Text : SharedMemoryValue
        {
            /// <summary>
            /// Text value
            /// </summary>
            public string Value { get; set; } = string.Empty;
            
            public override object Contents => Value;
        }

        /// <summary>
        /// Represents a number
        /// </summary>
        public class Number : SharedMemoryValue
        {
            /// <summary>
            /// Number value
            /// </summary>
            public decimal Value { get; set; }
            
            public override object Contents => Value;
        }

        /// <summary>
        /// Represents an absence of value
        /// </summary>
        public class Nothing : SharedMemoryValue
        {
            public override object Contents => null;
        }
        
        /// <summary>
        /// Raw contents of the value
        /// </summary>
        public abstract object Contents { get; }

        /// <summary>
        /// Creates new <see cref="SharedMemoryValue"/> of type <see cref="Error"/>
        /// </summary>
        public static SharedMemoryValue NewError(string message) => new Error { Message = message };

        /// <summary>
        /// Creates new <see cref="SharedMemoryValue"/> of type <see cref="Boolean"/>
        /// </summary>
        public static SharedMemoryValue NewBoolean(bool value) => new Boolean { Value = value };

        /// <summary>
        /// Creates new <see cref="SharedMemoryValue"/> of type <see cref="Text"/>
        /// </summary>
        public static SharedMemoryValue NewText(string value) => new Text { Value = value };

        /// <summary>
        /// Creates new <see cref="SharedMemoryValue"/> of type <see cref="Number"/>
        /// </summary>
        public static SharedMemoryValue NewNumber(decimal value) => new Number { Value = value };

        /// <summary>
        /// Creates new <see cref="SharedMemoryValue"/> of type <see cref="Nothing"/>
        /// </summary>
        public static SharedMemoryValue NewNothing() => new Nothing();
    }
}