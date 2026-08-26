#!/usr/bin/env bash
set -e

# 定位脚本所在目录与项目根目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

cd "$BACKEND_DIR"

echo "🧹 清理历史覆盖率测试数据..."
rm -rf test-results
rm -rf coveragereport

echo "🧪 运行全量单元测试并采集覆盖率..."
dotnet test SIASUN.RCS.sln \
  --settings coverlet.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory test-results

echo "📊 生成 HTML 可视化覆盖率大盘报告..."
reportgenerator \
  "-reports:test-results/**/coverage.cobertura.xml" \
  "-targetdir:coveragereport" \
  "-reporttypes:Html;TextSummary" \
  "-assemblyfilters:-*.Tests;-*.TestBase;-*.ConsoleTestApp" \
  "-filefilters:-**/Migrations/**;-*Designer.cs;-*ModelSnapshot.cs"

echo "✅ 覆盖率报告生成完毕！"
echo "📄 文本摘要如下："
cat coveragereport/Summary.txt

# 若在 macOS 环境且支持 open 命令，自动打开 HTML 报告
if command -v open &> /dev/null && [ -f "coveragereport/index.html" ]; then
  open coveragereport/index.html
fi

