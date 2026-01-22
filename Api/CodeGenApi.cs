using System;
using System.Collections.Generic;
using System.Linq;
using ReClassNET.CodeGenerator;
using McpPlugin.Server;
using ReClassNET.Nodes;
using ReClassNET.Plugins;

namespace McpPlugin.Api
{
    /// <summary>
    /// Code generation API for MCP.
    /// </summary>
    public class CodeGenApi
    {
        private readonly IPluginHost _host;

        public CodeGenApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>
        /// Generates C++ code for classes.
        /// </summary>
        [McpTool("generate_cpp", Description = "Generate C++ code for classes")]
        public object GenerateCpp(string[] classes = null)
        {
            try
            {
                var project = _host.MainWindow.CurrentProject;
                if (project == null)
                {
                    return new { error = "No project loaded" };
                }

                var targetClasses = GetTargetClasses(classes);
                if (targetClasses == null || !targetClasses.Any())
                {
                    return new { error = "No classes found" };
                }

                var generator = new CppCodeGenerator(project.TypeMapping);
                var code = GenerateCode(generator, targetClasses, project.Enums);

                return new
                {
                    language = "cpp",
                    classCount = targetClasses.Count,
                    code
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Generates C# code for classes.
        /// </summary>
        [McpTool("generate_csharp", Description = "Generate C# code for classes")]
        public object GenerateCSharp(string[] classes = null)
        {
            try
            {
                var project = _host.MainWindow.CurrentProject;
                if (project == null)
                {
                    return new { error = "No project loaded" };
                }

                var targetClasses = GetTargetClasses(classes);
                if (targetClasses == null || !targetClasses.Any())
                {
                    return new { error = "No classes found" };
                }

                var generator = new CSharpCodeGenerator();
                var code = GenerateCode(generator, targetClasses, project.Enums);

                return new
                {
                    language = "csharp",
                    classCount = targetClasses.Count,
                    code
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Exports a single class as code.
        /// </summary>
        [McpTool("export_class", Description = "Export a single class as code")]
        public object ExportClass(string @class, string format = "cpp")
        {
            try
            {
                var project = _host.MainWindow.CurrentProject;
                if (project == null)
                {
                    return new { error = "No project loaded" };
                }

                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { error = $"Class not found: {@class}" };
                }

                ICodeGenerator generator;
                if (format.Equals("csharp", StringComparison.OrdinalIgnoreCase) ||
                    format.Equals("cs", StringComparison.OrdinalIgnoreCase))
                {
                    generator = new CSharpCodeGenerator();
                }
                else
                {
                    generator = new CppCodeGenerator(project.TypeMapping);
                }

                var code = GenerateCode(generator, new[] { classNode }, project.Enums);

                return new
                {
                    @class = classNode.Name,
                    format,
                    code
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Gets type mappings for code generation.
        /// </summary>
        [McpTool("get_type_mappings", Description = "Get type name mappings for code generation")]
        public object GetTypeMappings()
        {
            try
            {
                var project = _host.MainWindow.CurrentProject;
                if (project == null)
                {
                    return new { error = "No project loaded" };
                }

                var mapping = project.TypeMapping;
                if (mapping == null)
                {
                    return new { error = "No type mapping defined" };
                }

                return new
                {
                    @float = mapping.TypeFloat,
                    @double = mapping.TypeDouble,
                    @bool = mapping.TypeBool,
                    int8 = mapping.TypeInt8,
                    int16 = mapping.TypeInt16,
                    int32 = mapping.TypeInt32,
                    int64 = mapping.TypeInt64,
                    nint = mapping.TypeNInt,
                    uint8 = mapping.TypeUInt8,
                    uint16 = mapping.TypeUInt16,
                    uint32 = mapping.TypeUInt32,
                    uint64 = mapping.TypeUInt64,
                    nuint = mapping.TypeNUInt,
                    utf8 = mapping.TypeUtf8Text,
                    utf16 = mapping.TypeUtf16Text,
                    utf32 = mapping.TypeUtf32Text,
                    functionPtr = mapping.TypeFunctionPtr,
                    vector2 = mapping.TypeVector2,
                    vector3 = mapping.TypeVector3,
                    vector4 = mapping.TypeVector4,
                    matrix3x3 = mapping.TypeMatrix3x3,
                    matrix3x4 = mapping.TypeMatrix3x4,
                    matrix4x4 = mapping.TypeMatrix4x4
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        private List<ClassNode> GetTargetClasses(string[] classNames)
        {
            var project = _host.MainWindow.CurrentProject;
            if (project == null) return null;

            if (classNames == null || classNames.Length == 0)
            {
                return project.Classes.ToList();
            }

            var result = new List<ClassNode>();
            foreach (var name in classNames)
            {
                var classNode = FindClass(name);
                if (classNode != null)
                {
                    result.Add(classNode);
                }
            }
            return result;
        }

        private ClassNode FindClass(string nameOrUuid)
        {
            var project = _host.MainWindow.CurrentProject;
            if (project == null) return null;

            if (Guid.TryParse(nameOrUuid, out var uuid))
            {
                var byUuid = project.Classes.FirstOrDefault(c => c.Uuid == uuid);
                if (byUuid != null) return byUuid;
            }

            return project.Classes.FirstOrDefault(c =>
                c.Name.Equals(nameOrUuid, StringComparison.OrdinalIgnoreCase));
        }

        private string GenerateCode(ICodeGenerator generator, IEnumerable<ClassNode> classes,
            IEnumerable<ReClassNET.Project.EnumDescription> enums)
        {
            // Use the ICodeGenerator.GenerateCode method
            return generator.GenerateCode(
                classes.ToList(),
                enums.ToList(),
                _host.Logger);
        }
    }
}
