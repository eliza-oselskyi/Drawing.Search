using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace

namespace DotNext; // this type extends the DotNext library, so keep this namespace!

/// <summary>
/// Represents a result of an operation which can be a successful value or a custom error.
/// </summary>
/// <typeparam name="T">The type of the successful result.</typeparam>
/// <typeparam name="TError">The type of the custom error. Default value must represent the successful result.</typeparam>
[Serializable]
[StructLayout(LayoutKind.Auto)]
public readonly struct Result<T, TError> : ISerializable
    where TError : struct, Enum
{
    private const string ValueSerData = "Value";
    private const string ErrorSerData = "Error";
    private const string IsSuccessfulSerData = "IsSuccessful";

    private readonly T value;
    private readonly TError error;
    private readonly bool isSuccessful;

    /// <summary>
    /// Initializes a new successful result.
    /// </summary>
    /// <param name="value">The value to be stored as result.</param>
    public Result(T value)
    {
        this.value = value;
        this.error = default;
        isSuccessful = true;
    }

    /// <summary>
    /// Initializes a new unsuccessful result with a custom error.
    /// </summary>
    /// <param name="error">The custom error representing failure.</param>
    public Result(TError error)
    {
        this.value = default!;
        this.error = error;
        isSuccessful = false;
    }

    [SuppressMessage("Usage", "CA1801", Justification = "context is required by .NET serialization framework")]
    private Result(SerializationInfo info, StreamingContext context)
    {
        value = (T)info.GetValue(ValueSerData, typeof(T));
        error = (TError)info.GetValue(ErrorSerData, typeof(TError));
        isSuccessful = info.GetBoolean(IsSuccessfulSerData);
    }

    /// <summary>
    /// Indicates that the result is successful.
    /// </summary>
    /// <value><see langword="true"/> if this result is successful; <see langword="false"/> if this result represents an error.</value>
    public bool IsSuccessful => isSuccessful;

    /// <summary>
    /// Extracts the actual result.
    /// </summary>
    /// <exception cref="InvalidOperationException">This result is not successful.</exception>
    public T Value
    {
        get
        {
            Validate();
            return value;
        }
    }


    /// <summary>
    /// Gets the custom error associated with this result.
    /// </summary>
    /// <exception cref="InvalidOperationException">This result is successful (no error).</exception>
    public TError Error => error;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Validate()
    {
        if (!isSuccessful)
            Throw();
    }

    //[DoesNotReturn] // TODO: Uncomment this when on netstandard2.0
    private void Throw() => throw new UndefinedResultException<TError>(Error);

    /// <summary>
    /// If a successful result is present, applies the provided mapping function, propagating errors.
    /// </summary>
    /// <param name="converter">A mapping function that returns a result (allows custom error handling).</param>
    /// <typeparam name="U">The type of the result of the mapping function.</typeparam>
    /// <returns>The conversion result.</returns>
    public Result<U, TError> Convert<U>(Func<T, Result<U, TError>> converter)
    {
        return isSuccessful ? converter(value) : new Result<U, TError>(error);
    }

    /// <summary>
    /// If a successful result is present, applies the binding function to the result.
    /// </summary>
    public Result<U, TError> Bind<U>(Func<T, Result<U, TError>> bind) => Convert(bind);

    /// <summary>
    /// If successful, applies the mapping function to the result.
    /// </summary>
    public Result<U, TError> Map<U>(Func<T, U> map) =>
        isSuccessful ? new Result<U, TError>(map(value)) : new Result<U, TError>(error);

    /// <summary>
    /// Attempts to extract the value if it is present.
    /// </summary>
    /// <param name="v">Extracted value.</param>
    /// <returns><see langword="true"/> if value is present; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(out T v)
    {
        v = this.value;
        return isSuccessful;
    }

    /// <summary>
    /// Attempts to extract the error if it is present.
    /// </summary>
    /// <param name="e">Extracted error.</param>
    /// <returns><see langword="true"/> if error is present; otherwise, <see langword="false"/>.</returns>
    public bool TryGetError(out TError e)
    {
        e = this.error;
        return !isSuccessful;
    }

    /// <summary>
    /// Returns the value if present; otherwise returns a default value.
    /// </summary>
    /// <param name="defaultValue">The value to be returned if this result is unsuccessful.</param>
    /// <returns>The value, if present, otherwise <paramref name="defaultValue"/>.</returns>
    public T Or(T defaultValue) => isSuccessful ? value : defaultValue;

    /// <summary>
    /// Returns the value if present; otherwise returns a default value from the error-mapping function.
    /// </summary>
    /// <param name="errorMap">The function to map the error to a default value.</param>
    /// <returns>The value, if present, otherwise the result of <paramref name="errorMap"/>.</returns>
    public T Or (Func<TError, T> errorMap) => isSuccessful ? value : errorMap(error);

    /// <summary>
    /// Returns this result if successful; otherwise returns an unsuccessful result with the provided error value.
    /// </summary>
    /// <param name="errorValue">The error value to use when this result is unsuccessful.</param>
    /// <returns>This result if successful; otherwise an unsuccessful result with the provided error value.</returns>
    public Result<T, TError> Or(TError errorValue) => isSuccessful ? this : errorValue;

    /// <summary>
    /// Returns the value if present; otherwise returns default value.
    /// </summary>
    /// <returns>The value, if present, otherwise <c>default</c>.</returns>
    public T OrDefault() => value;

    /// <summary>
    /// Returns the value if present; otherwise invokes the delegate.
    /// </summary>
    /// <param name="defaultFunc">A delegate to be invoked if value is not present.</param>
    /// <returns>The value, if present, otherwise returned from delegate.</returns>
    public T OrInvoke(Func<T> defaultFunc) => isSuccessful ? value : defaultFunc();

    /// <summary>
    /// Returns the value if present; otherwise invokes the delegate with the error.
    /// </summary>
    /// <param name="defaultFunc">A delegate to be invoked if value is not present.</param>
    /// <returns>The value, if present, otherwise returned from delegate.</returns>
    public T OrInvoke(Func<TError, T> defaultFunc) => isSuccessful ? value : defaultFunc(error);

    /// <summary>
    /// Returns the value if present; otherwise throws the provided exception.
    /// </summary>
    /// <param name="exception">The exception to throw if value is not present.</param>
    /// <returns>The value, if present, otherwise throw provided exception.</returns>
    public T OrThrow(Exception exception) => isSuccessful ? value : throw exception;

    /// <summary>
    /// Converts this result into <see cref="Result{T}"/>.
    /// </summary>
    /// <returns>The converted result.</returns>
    public Result<T> ToResult() => IsSuccessful ? new Result<T>(value) : new Result<T>(new UndefinedResultException<TError>(Error));


    /// <summary>
    /// Extracts the actual result.
    /// </summary>
    /// <param name="result">The result object.</param>
    public static explicit operator T(in Result<T, TError> result) => result.Value;

    /// <summary>
    /// Converts value into the result.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    public static implicit operator Result<T, TError>(T value) => new Result<T, TError>(value);

    /// <summary>
    /// Converts error into the result.
    /// </summary>
    /// <param name="error">The error to be converted.</param>
    public static implicit operator Result<T, TError>(TError error) => new Result<T, TError>(error);

    /// <summary>
    /// Indicates that both results are successful.
    /// </summary>
    /// <param name="left">The first result to check.</param>
    /// <param name="right">The second result to check.</param>
    /// <returns><see langword="true"/> if both results are successful; otherwise, <see langword="false"/>.</returns>
    public static bool operator &(in Result<T, TError> left, in Result<T, TError> right) => left.isSuccessful && right.isSuccessful;

    /// <summary>
    /// Indicates that the result is successful.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <returns><see langword="true"/> if this result is successful; <see langword="false"/> if this result represents error.</returns>
    public static bool operator true(in Result<T, TError> result) => result.isSuccessful;

    /// <summary>
    /// Indicates that the result represents error.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <returns><see langword="false"/> if this result is successful; <see langword="true"/> if this result represents error.</returns>
    public static bool operator false(in Result<T, TError> result) => !result.isSuccessful;

    /// <inheritdoc cref="operator false(in Result{T, TError})"/>
    public static bool operator !(in Result<T, TError> result) => !result.IsSuccessful;

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(ValueSerData, value, typeof(T));
        info.AddValue(ErrorSerData, error, typeof(TError));
        info.AddValue(IsSuccessfulSerData, isSuccessful);
    }

    /// <summary>
    /// Returns textual representation of this object.
    /// </summary>
    /// <returns>The textual representation of this object.</returns>
    public override string ToString() => isSuccessful ? value?.ToString() ?? "<NULL>" : error.ToString() ?? "<ERROR>";
}

/// <summary>
/// Extensions for working with the Result type.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Returns a successful result from provided value.
    /// </summary>
    public static Result<T> ResultFromValue<T>(this T value) => new(value);

    /// <summary>
    /// Returns a successful result from provided value.
    /// </summary>
    /// <param name="value">The provided value.</param>
    /// <typeparam name="T">The provided value type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    public static Result<T, TError> ResultFromValue<T, TError>(this T value) where TError : struct, Enum => new(value);

    /// <summary>
    /// Returns an unsuccessful result from provided error.
    /// </summary>
    /// <param name="error">The provided error.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <returns>An unsuccessful result wrapping the provided error.</returns>
    public static Result<T, TError> ResultFromError<T, TError>(this TError error) where TError : struct, Enum => new(error);


    /// <summary>
    /// Returns an unsuccessful result from provided exception.
    /// </summary>
    /// <param name="e">The provided exception.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>An unsuccessful result wrapping the provided exception.</returns>
    public static Result<T> ResultFromException<T>(this Exception e) => new(e);

    /// <summary>
    /// Returns a boxed value result from provided result.
    /// </summary>
    /// <param name="result">The provided result.</param>
    /// <typeparam name="T">The value type of the provided result.</typeparam>
    public static Result<object?> Box<T>(this Result<T> result) => result.IsSuccessful ? new(result.Value) : new(result.Error);
    /// <summary>
    /// Returns a boxed value result from provided result.
    /// </summary>
    /// <param name="result">The provided result.</param>
    /// <typeparam name="T">The value type of the provided result.</typeparam>
    /// <typeparam name="TError">The error type of the provided result.</typeparam>
    public static Result<object?, TError> Box<T, TError>(this Result<T, TError> result) where TError : struct, Enum => result.IsSuccessful ? new(result.Value) : new(result.Error);

    /// <summary>
    /// Checks if the provided result's value is null.
    /// Returns an unsuccessful result with NullReferenceException if value is null.
    /// Otherwise, returns the provided result unchanged.
    /// </summary>
    /// <param name="result">The provided result.</param>
    /// <typeparam name="T">The value type.</typeparam>
    public static Result<T> EnsureNotNull<T>(this Result<T?> result) where T : class
        => result.EnsureNotNull<T, NullReferenceException>();

    /// <summary>
    /// Checks if the provided result's value is null.
    /// Returns an unsuccessful result with the provided exception if value is null.
    /// Otherwise, returns the provided result.
    /// </summary>
    /// <param name="result">The provided result.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <returns>A non-null successful result, or an unsuccessful result if the value was null or the input was unsuccessful.</returns>
    public static Result<T> EnsureNotNull<T, TException>(this Result<T?> result)
        where T : class
        where TException : Exception, new() =>
        result.IsSuccessful
            ? result.Value is null
                ? ExceptionDispatchInfo.Capture(new TException()).SourceException.ResultFromException<T>()
                : new Result<T>(result.Value)
            : new Result<T>(result.Error);

    /// <summary>
    /// Checks if the provided result's value is null.
    /// Returns an unsuccessful result with the provided error if value is null.
    /// Otherwise, returns the provided result unchanged.
    /// </summary>
    /// <param name="result">The provided result.</param>
    /// <param name="error">The error to return when the successful value is null.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    public static Result<T, TError> EnsureNotNull<T, TError>(this Result<T?, TError> result, TError error)
        where T : class
        where TError : struct, Enum =>
        result.IsSuccessful
            ? result.Value is null
                ? new Result<T, TError>(error)
                : new Result<T, TError>(result.Value)
            : new Result<T, TError>(result.Error);

    /// <summary>
    /// If a successful result is present, applies the binding function and returns it as a result.
    /// Otherwise, propagates the error.
    /// </summary>
    public static Result<TResult> Bind<T, TResult>(this Result<T> result, Func<T, Result<TResult>> bind) => bind is null
        ? throw new ArgumentNullException(nameof(bind))
        : result.IsSuccessful ? bind(result.Value) : new Result<TResult>(result.Error);

    /// <summary>
    /// If a successful result is present, applies the binding function and returns it as a result.
    /// Otherwise, propagates the error.
    /// </summary>
    public static Result<TResult, TError> Bind<T, TResult, TError>(this Result<T, TError> result, Func<T, Result<TResult, TError>> bind) where TError : struct, Enum => bind is null
        ? throw new ArgumentNullException(nameof(bind))
        : result.IsSuccessful ? bind(result.Value) : new Result<TResult, TError>(result.Error);

    /// <summary>
    /// If a successful result is present, applies the mapping function and returns it as a result.
    /// </summary>
    public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> map) => map is null
        ? throw new ArgumentNullException(nameof(map))
        : result.Bind(v => map(v).ResultFromValue());

    /// <summary>
    /// If a successful result is present, applies the mapping function and returns it as a result.
    /// </summary>
    public static Result<TResult, TError> Map<T, TResult, TError>(this Result<T, TError> result, Func<T, TResult> map) where TError : struct, Enum => map is null
        ? throw new ArgumentNullException(nameof(map))
        : result.Bind<T, TResult, TError>(v => map(v).ResultFromValue<TResult, TError>());

}

/// <summary>
/// Extensions for working with the Optional type.
/// </summary>
public static class Option
{
    /// <summary>
    /// Returns a Some(value) from provided value.
    /// </summary>
    public static Optional<T> Some<T>(this T value) => new(value);

    /// <summary>
    /// Returns a None from provided value.
    /// </summary>
    /// <returns></returns>
    public static Optional<T> None<T>(this T _) => new();

    /// <summary>
    ///  Returns a None.
    /// </summary>
    public static Optional<T> None<T>() => new();

    /// <summary>
    /// Returns option some if the value is not null; otherwise option none.
    /// </summary>
    public static Optional<T> SomeNotNull<T>(this T? value) where T : class => value ?? Optional<T>.Empty;

    /// <summary>
    /// Returns option some for a nullable value type if it has a value; otherwise option none.
    /// </summary>
    public static Optional<T> SomeNotNull<T>(this T? value) where T : struct => value?.Some() ?? Optional<T>.Empty;

    /// <summary>
    /// Pattern matches on the optional.
    /// </summary>
    /// <param name="option">The optional to match.</param>
    /// <param name="some">The function to apply to the value if present.</param>
    /// <param name="none">The function to apply if the optional is None.</param>
    public static TResult Match<T, TResult>(this Optional<T> option, Func<T, TResult> some, Func<TResult> none)
    {
        if (some is null) throw new ArgumentNullException(nameof(some));
        if (none is null) throw new ArgumentNullException(nameof(none));

        return !option.IsPresent ? none() : some(option.Value);
    }

    /// <summary>
    /// If a value is present, applies the mapping function to it and returns the result as an optional.
    /// </summary>
    public static Optional<TResult> Map<T, TResult>(this Optional<T> option, Func<T, TResult> map) => map is null
        ? throw new ArgumentNullException(nameof(map))
        : option.Bind(v => map(v).Some());

    /// <summary>
    /// If a value is present, applies the binding function to it and returns the result as an optional.
    /// </summary>
    public static Optional<TResult> Bind<T, TResult>(this Optional<T> option, Func<T, Optional<TResult>> bind) => bind is null
        ? throw new ArgumentNullException(nameof(bind))
        : option.IsPresent ? bind(option.Value) : Optional<TResult>.Empty;
}

/// <summary>
/// Indicates that the result of the operation is unavailable.
/// </summary>
/// <typeparam name="TError">The type of the error code.</typeparam>
public class UndefinedResultException<TError> : Exception
    where TError : struct, Enum
{
    internal UndefinedResultException(TError errorCode) : base("NoResult") => ErrorCode = errorCode;

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public TError ErrorCode { get; }
}
