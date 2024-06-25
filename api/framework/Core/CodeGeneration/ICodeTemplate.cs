using CodegenCS;

namespace FSH.Framework.Infrastructure.CodeGeneration;

public interface ICodeTemplate
{
    void Generate(CodegenTextWriter writer);
}
