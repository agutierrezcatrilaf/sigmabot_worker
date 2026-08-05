// Prueba local de acceso SMB vía "net use". Quitar credenciales en duro antes de commitear si no deben quedar en el repo.
using System.Diagnostics;
using System.IO;

// Mismo recurso que en la referencia por imagen: \\172.20.1.89\plano_aconex
const string ServerPath = @"\\172.20.1.89\planos_aconex";
const string User = "CORPORATIVO\\integra_Aconex_extr";
const string Pass = "Aconex2003.,";

Console.WriteLine("=== NetShare smoke test ===");
Console.WriteLine($"Recurso: {ServerPath}");
Console.WriteLine($"Usuario: {User}");
Console.WriteLine();

// 1. Conectar (misma idea que el ejemplo con ProcessStartInfo)
var connectArgs = $"use \"{ServerPath}\" \"{Pass}\" /user:\"{User}\"";
var connectSi = new ProcessStartInfo("net.exe", connectArgs)
{
    CreateNoWindow = true,
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};

using var connectProc = Process.Start(connectSi);
if (connectProc == null)
{
    Console.WriteLine("ERROR: no se pudo iniciar net.exe");
    return 1;
}

var connectOut = await connectProc.StandardOutput.ReadToEndAsync();
var connectErr = await connectProc.StandardError.ReadToEndAsync();
await connectProc.WaitForExitAsync();

Console.WriteLine($"net use → código salida: {connectProc.ExitCode}");
if (!string.IsNullOrWhiteSpace(connectOut))
    Console.WriteLine(connectOut.TrimEnd());
if (!string.IsNullOrWhiteSpace(connectErr))
    Console.WriteLine(connectErr.TrimEnd());

if (connectProc.ExitCode != 0)
{
    Console.WriteLine();
    Console.WriteLine("La conexión falló; no se intenta listar archivos.");
    return connectProc.ExitCode;
}

try
{
    // 2. Crear archivo de prueba en la raíz del share
    Console.WriteLine();
    Console.WriteLine("--- Escritura de prueba ---");
    var probeName = $"smoke-write-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
    var probePath = Path.Combine(ServerPath, probeName);
    var probeContent = $"Smoke test desde {Environment.MachineName} en {DateTime.Now:O}";
    await File.WriteAllTextAsync(probePath, probeContent);
    Console.WriteLine($"Archivo creado: {probeName}");

    // 3. Listar (como en la referencia: Directory tras net use)
    Console.WriteLine();
    Console.WriteLine("--- Listado (primeros 20 archivos) ---");
    // En UNC, Directory.Exists también devuelve false para "Access denied" u otros errores.
    // Preferimos intentar enumerar y reportar el error real.
    IReadOnlyList<string> files = Array.Empty<string>();
    Exception? lastError = null;
    for (var attempt = 1; attempt <= 3; attempt++)
    {
        try
        {
            files = Directory.GetFiles(ServerPath);
            lastError = null;
            break;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            lastError = ex;
            Console.WriteLine($"Intento {attempt}/3 al listar falló: {ex.GetType().Name} - {ex.Message}");
            if (attempt < 3)
                await Task.Delay(500);
        }
    }

    if (lastError != null)
    {
        Console.WriteLine();
        Console.WriteLine("No se pudo enumerar el recurso compartido, aunque net use devolvió OK.");
        Console.WriteLine("Suele indicar permisos de listado insuficientes o restricciones del share/NTFS.");
        return 2;
    }

    Console.WriteLine($"Total archivos en raíz del recurso: {files.Count}");
    foreach (var f in files.Take(20))
        Console.WriteLine("  " + Path.GetFileName(f));
    if (files.Count > 20)
        Console.WriteLine("  ...");
}
finally
{
    // 4. Desconectar
    Console.WriteLine();
    Console.WriteLine("Desconectando recurso...");
    var deleteSi = new ProcessStartInfo("net.exe", $"use \"{ServerPath}\" /delete /y")
    {
        CreateNoWindow = true,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    using var del = Process.Start(deleteSi);
    if (del != null)
    {
        var o = await del.StandardOutput.ReadToEndAsync();
        var e = await del.StandardError.ReadToEndAsync();
        await del.WaitForExitAsync();
        if (!string.IsNullOrWhiteSpace(o)) Console.WriteLine(o.TrimEnd());
        if (!string.IsNullOrWhiteSpace(e)) Console.WriteLine(e.TrimEnd());
        Console.WriteLine($"net use /delete → código salida: {del.ExitCode}");
    }
}

Console.WriteLine();
Console.WriteLine("Listo.");
return 0;
