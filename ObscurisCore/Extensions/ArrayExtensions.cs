namespace ObscurisCore.Extensions
{
    /// <summary>
    /// Provides extension methods for arrays that enable iteration with index and conditional execution of actions on
    /// array elements.
    /// </summary>
    /// <remarks>The methods in this class allow callers to perform actions on each element of an array,
    /// optionally including the element's index and supporting conditional execution. These extensions are intended to
    /// simplify common array iteration patterns and improve code readability. All methods operate on arrays in index
    /// order, from 0 to array.Length - 1. The class is static and cannot be instantiated.</remarks>
    public static class ArrayExtensions
    {
        /// <summary>
        /// Invokes the specified action for each element in the array, providing the element's index and value.
        /// </summary>
        /// <remarks>This method is an extension for arrays, allowing iteration with both the index and
        /// value of each element. The action is called once for each element in the array, in order from index 0 to
        /// array.Length - 1. If the array is empty, the action is not invoked.</remarks>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="array">The array whose elements will be passed to the action. Cannot be null.</param>
        /// <param name="action">The action to invoke for each element, receiving the index and the element value. Cannot be null.</param>
        public static void For<T>(this T[] array, Action<int, T> action)
        {
            for (var i = 0; i < array.Length; i++)
            {
                action(i, array[i]);
            }
        }

        /// <summary>
        /// Executes the specified action for each element in the array if the given condition is <see
        /// langword="false"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="array">The array whose elements the action will be applied to if the condition is <see langword="false"/>.</param>
        /// <param name="condition">A Boolean value that determines whether the action should be skipped. If <see langword="true"/>, the action
        /// is not executed.</param>
        /// <param name="action">The action to perform on each element of the array. The first parameter is the index of the element; the
        /// second is the element itself.</param>
        public static void ForIfNot<T>(this T[] array, bool condition, Action<int, T> action)
        {
            if (!condition)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    action(i, array[i]);
                }
            }
        }
    }
}