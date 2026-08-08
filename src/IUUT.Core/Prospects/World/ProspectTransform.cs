namespace IUUT.Core.Prospects.World;

/// <summary>
/// An actor's world placement, decoded from the <c>ActorTransform</c> struct.
/// Units are Unreal centimetres, so divide by 100 for metres.
/// </summary>
public sealed record ProspectTransform(
    float X,
    float Y,
    float Z,
    float RotationX,
    float RotationY,
    float RotationZ,
    float RotationW,
    float ScaleX,
    float ScaleY,
    float ScaleZ)
{
    /// <summary>The world position in metres, for display.</summary>
    public (double X, double Y, double Z) Metres => (X / 100.0, Y / 100.0, Z / 100.0);

    /// <summary>Planar distance in metres to another placement (ignores height).</summary>
    public double DistanceTo(ProspectTransform other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var dx = (double)X - other.X;
        var dy = (double)Y - other.Y;
        return Math.Sqrt((dx * dx) + (dy * dy)) / 100.0;
    }
}

/// <summary>
/// Decodes an actor's <c>ActorTransform</c>. Despite being a UE <c>Transform</c> — which the
/// generic reader treats as an opaque native struct, because most engines serialise it raw —
/// Icarus writes it as TAGGED sub-properties: <c>Rotation</c> (Quat, 16 bytes),
/// <c>Translation</c> (Vector, 12), and <c>Scale3D</c> (Vector, 12).
/// <para>
/// This is a targeted reader rather than a change to <see cref="UePropertyReader"/>'s native-struct
/// set on purpose: that set is shared with the blob WRITER, and widening it would alter parsing for
/// every already-shipped blob edit. Decoding on demand keeps those gated paths untouched.
/// </para>
/// <para>
/// Note for the write phase: <c>Translation</c> is a fixed 12 bytes, so rebasing a structure's
/// position is an in-place, size-preserving write — the same low-risk class as the quest reset,
/// with no length fixup required.
/// </para>
/// </summary>
public static class ProspectTransformReader
{
    /// <summary>The byte offset of the translation vector within the blob, or null when absent.</summary>
    public static int? TranslationOffset(byte[] data, UeProperty actorTransform) =>
        FindField(data, actorTransform, "Translation", expectedSize: 12);

    /// <summary>
    /// Decodes <paramref name="actorTransform"/> (a node named <c>ActorTransform</c>), or returns
    /// null when the value does not carry the expected tagged fields.
    /// </summary>
    public static ProspectTransform? Read(byte[] data, UeProperty actorTransform)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(actorTransform);

        var translation = FindField(data, actorTransform, "Translation", expectedSize: 12);
        if (translation is not { } t)
        {
            return null; // position is the point of this decode; without it there is nothing useful
        }

        var rotation = FindField(data, actorTransform, "Rotation", expectedSize: 16);
        var scale = FindField(data, actorTransform, "Scale3D", expectedSize: 12);

        return new ProspectTransform(
            BitConverter.ToSingle(data, t),
            BitConverter.ToSingle(data, t + 4),
            BitConverter.ToSingle(data, t + 8),
            rotation is { } r ? BitConverter.ToSingle(data, r) : 0f,
            rotation is { } r1 ? BitConverter.ToSingle(data, r1 + 4) : 0f,
            rotation is { } r2 ? BitConverter.ToSingle(data, r2 + 8) : 0f,
            rotation is { } r3 ? BitConverter.ToSingle(data, r3 + 12) : 1f,
            scale is { } s ? BitConverter.ToSingle(data, s) : 1f,
            scale is { } s1 ? BitConverter.ToSingle(data, s1 + 4) : 1f,
            scale is { } s2 ? BitConverter.ToSingle(data, s2 + 8) : 1f);
    }

    // Walks the transform's own tagged-property list and returns the value offset of one field.
    private static int? FindField(byte[] data, UeProperty actorTransform, string fieldName, int expectedSize)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(actorTransform);

        var pos = actorTransform.ValueOffset;
        var end = actorTransform.ValueOffset + actorTransform.ValueSize;
        if (end > data.Length)
        {
            return null;
        }

        while (pos < end)
        {
            var name = UePropertyReader.ReadFString(data, ref pos);
            if (string.IsNullOrEmpty(name) || string.Equals(name, "None", StringComparison.Ordinal))
            {
                return null;
            }

            var type = UePropertyReader.ReadFString(data, ref pos);
            if (string.IsNullOrEmpty(type) || pos + 8 > end)
            {
                return null;
            }

            var size = BitConverter.ToInt32(data, pos);
            pos += 8; // Size + ArrayIndex

            if (string.Equals(type, "StructProperty", StringComparison.Ordinal))
            {
                UePropertyReader.ReadFString(data, ref pos); // struct name
                pos += 16;                                   // struct Guid
            }

            pos += 1; // HasPropertyGuid

            if (size < 0 || pos + size > end)
            {
                return null;
            }

            if (string.Equals(name, fieldName, StringComparison.Ordinal) && size >= expectedSize)
            {
                return pos;
            }

            pos += size;
        }

        return null;
    }
}
