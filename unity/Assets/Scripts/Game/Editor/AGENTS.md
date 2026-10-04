# Game/Editor (ajan notu)

Kök `AGENTS.md`. Menüler `Dovus/...`; Play Sweep + binders. Capture arşivi `archive/capture-scripts`.
Başsız tarama: `tools/SweepV2`. MCP ile Unity build yasak.
Batchmode (açık editörün projesinde çalışmaz): eksik script `Unity.exe -batchmode -projectPath <proje> -executeMethod Dovus.Game.Editor.MissingScriptAudit.Run -auditOut <dosya> -logFile <log>`;
PlayMode smoke `Unity.exe -batchmode -projectPath <proje> -runTests -testPlatform PlayMode -testResults <xml> -smokeOut <dosya> -logFile <log>`.
