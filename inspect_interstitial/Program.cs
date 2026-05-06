using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

class Program
{
    static void Main()
    {
        var path = @"C:/Users/Muhammed/.nuget/packages/xamarin.googleplayservices.ads.api/124.9.0.1/lib/net9.0-android35.0/Xamarin.GooglePlayServices.Ads.Api.dll";
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var pe = new PEReader(fs);
        var reader = pe.GetMetadataReader();

        foreach (var typeDefHandle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(typeDefHandle);
            var ns = reader.GetString(typeDef.Namespace);
            var name = reader.GetString(typeDef.Name);
            if (name.Contains("InterstitialAdLoadCallback") && ns.Contains("Ads"))
            {
                Console.WriteLine($"Type: {ns}.{name}");
                Console.WriteLine($"  Base: {GetTypeName(reader, typeDef.BaseType)}");
                foreach (var methodHandle in typeDef.GetMethods())
                {
                    var method = reader.GetMethodDefinition(methodHandle);
                    var methodName = reader.GetString(method.Name);
                    var sigReader = reader.GetBlobReader(method.Signature);
                    sigReader.ReadByte(); // header
                    int paramCount = sigReader.ReadCompressedInteger();
                    var retType = ReadType(ref sigReader, reader);
                    var paramTypes = new string[paramCount];
                    for (int i = 0; i < paramCount; i++)
                        paramTypes[i] = ReadType(ref sigReader, reader);
                    Console.WriteLine($"    Method: {methodName}({string.Join(", ", paramTypes)}) -> {retType}");
                }
            }
        }
    }

    static string ReadType(ref BlobReader sigReader, MetadataReader reader)
    {
        var kind = sigReader.ReadByte();
        switch (kind)
        {
            case 0x01: return "void";
            case 0x02: return "bool";
            case 0x08: return "int8";
            case 0x09: return "uint8";
            case 0x0A: return "int16";
            case 0x0B: return "uint16";
            case 0x0C: return "int32";
            case 0x0D: return "uint32";
            case 0x0E: return "int64";
            case 0x0F: return "uint64";
            case 0x10: return "float";
            case 0x11: return "double";
            case 0x12: return "string";
            case 0x13: return "object";
            case 0x16: return "typedref";
            case 0x18: return "native int";
            case 0x19: return "native uint";
            case 0x1C: return "object";
            case 0x55: // modifier
                var token = sigReader.ReadCompressedInteger();
                var handle = System.Reflection.Metadata.Ecma335.MetadataTokens.Handle(token);
                return $"mod({GetHandleName(reader, handle)})";
            case 0x0F1: return "sentinel";
            case 0x1D: // array
            case 0x1E: // szarray
                var elem = ReadType(ref sigReader, reader);
                return elem + "[]";
            default:
                if ((kind & 0x80) != 0)
                {
                    // complex types
                    switch (kind & 0x7F)
                    {
                        case 0x12: // class
                        case 0x11: // valuetype
                            var t = sigReader.ReadCompressedInteger();
                            var h = System.Reflection.Metadata.Ecma335.MetadataTokens.Handle(t);
                            return GetHandleName(reader, h);
                        case 0x10: // genericinst
                            var baseKind = sigReader.ReadByte();
                            var baseToken = sigReader.ReadCompressedInteger();
                            var baseHandle = System.Reflection.Metadata.Ecma335.MetadataTokens.Handle(baseToken);
                            var genericCount = sigReader.ReadCompressedInteger();
                            var args = new string[genericCount];
                            for (int i = 0; i < genericCount; i++)
                                args[i] = ReadType(ref sigReader, reader);
                            return $"{GetHandleName(reader, baseHandle)}<{string.Join(",", args)}>";
                        case 0x14: // var (generic parameter)
                            var varNum = sigReader.ReadCompressedInteger();
                            return $"T{varNum}";
                    }
                }
                return $"0x{kind:X2}";
        }
    }

    static string GetHandleName(MetadataReader reader, Handle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
                var tr = reader.GetTypeReference((TypeReferenceHandle)handle);
                return $"{reader.GetString(tr.Namespace)}.{reader.GetString(tr.Name)}";
            case HandleKind.TypeDefinition:
                var td = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                return $"{reader.GetString(td.Namespace)}.{reader.GetString(td.Name)}";
            default:
                return handle.Kind.ToString();
        }
    }

    static string GetTypeName(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
                var tr = reader.GetTypeReference((TypeReferenceHandle)handle);
                return $"{reader.GetString(tr.Namespace)}.{reader.GetString(tr.Name)}";
            case HandleKind.TypeDefinition:
                var td = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                return $"{reader.GetString(td.Namespace)}.{reader.GetString(td.Name)}";
            default:
                return handle.Kind.ToString();
        }
    }
}
