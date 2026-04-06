using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.OleDb;
using System.Runtime.InteropServices;

namespace pryZarateSP2._2
{
    public partial class frmPrincipal : Form
    {
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnMigrar;

        private readonly string dataFolder = Path.Combine(Application.StartupPath, "Data");
        private readonly string categoriasFileName = "Categorias.txt";
        private readonly string articulosFileName = "Articulos.txt";
        private readonly string databaseFileName = "Distribuidora.accdb";

        public frmPrincipal()
        {
            InitializeComponent();
        }

        private void Log(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private void btnMigrar_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            try
            {
                Directory.CreateDirectory(dataFolder);
                CreateSampleFilesIfMissing();
                var dbPath = Path.Combine(dataFolder, databaseFileName);
                string provider = null;
                if (!File.Exists(dbPath))
                {
                    Log("Base de datos no encontrada. Creando... ");
                    provider = CreateAccessDatabase(dbPath);
                    Log($"Base de datos creada (provider: {provider}).");
                }
                else
                {
                    provider = DetectProviderForOpen(dbPath);
                    Log($"Usando provider detectado: {provider}");
                }

                var connString = $"Provider={provider};Data Source={dbPath};Persist Security Info=False;";
                using (var conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    EnsureTables(conn);
                    ImportCategorias(conn);
                    ImportArticulos(conn);
                    conn.Close();
                }

                Log("Migración finalizada.");
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
            }
        }

        private void CreateSampleFilesIfMissing()
        {
            var catPath = Path.Combine(dataFolder, categoriasFileName);
            var artPath = Path.Combine(dataFolder, articulosFileName);

            if (!File.Exists(catPath))
            {
                File.WriteAllLines(catPath, new[] { "1|Placas", "2|Discos", "3|Memorias" });
                Log("Archivo 'Categorias.txt' creado de ejemplo.");
            }
            if (!File.Exists(artPath))
            {
                File.WriteAllLines(artPath, new[] {
                    "1|Motherboard ASUS|1|250.00",
                    "2|Disco Seagate 1TB|2|120.50",
                    "3|Memoria Kingston 8GB|3|75.00"
                });
                Log("Archivo 'Articulos.txt' creado de ejemplo.");
            }
        }

        private string CreateAccessDatabase(string path)
        {
            var aceProviders = new[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" };
            var progId = "ADOX.Catalog";
            var catType = Type.GetTypeFromProgID(progId);

            if (catType != null)
            {
                foreach (var prov in aceProviders)
                {
                    object cat = null;
                    try
                    {
                        cat = Activator.CreateInstance(catType);
                        var connStr = $"Provider={prov};Data Source={path};";
                        try
                        {
                            catType.InvokeMember("Create", System.Reflection.BindingFlags.InvokeMethod, null, cat, new object[] { connStr });
                            Log($"Base de datos creada mediante ADOX.Catalog con provider {prov}.");
                            return prov;
                        }
                        catch (Exception ex)
                        {
                            var inner = (ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException.Message : ex.Message;
                            Log($"ADOX.Catalog.Create con {prov} falló: {inner}");
                        }
                    }
                    finally
                    {
                        if (cat != null)
                            Marshal.ReleaseComObject(cat);
                    }
                }
            }
            else
            {
                Log("ADOX.Catalog no disponible en el sistema.");
            }

            var accessProgId = "Access.Application";
            var appType = Type.GetTypeFromProgID(accessProgId);
            if (appType != null)
            {
                object app = null;
                try
                {
                    app = Activator.CreateInstance(appType);
                    try
                    {
                        appType.InvokeMember("NewCurrentDatabase", System.Reflection.BindingFlags.InvokeMethod, null, app, new object[] { path });
                        Log("Base de datos creada mediante Access.Application.NewCurrentDatabase.");
                        try { appType.InvokeMember("Quit", System.Reflection.BindingFlags.InvokeMethod, null, app, null); } catch { }
                        // Assume ACE 16 if Access exists
                        return "Microsoft.ACE.OLEDB.16.0";
                    }
                    catch (Exception ex)
                    {
                        var inner = (ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException.Message : ex.Message;
                        Log($"Access.Application.NewCurrentDatabase falló: {inner}");
                    }
                }
                finally
                {
                    if (app != null)
                        Marshal.ReleaseComObject(app);
                }
            }
            else
            {
                Log("Access.Application no disponible en el sistema.");
            }

            try
            {
                var progJet = "ADOX.Catalog";
                var catTypeJet = Type.GetTypeFromProgID(progJet);
                if (catTypeJet != null)
                {
                    object catJet = Activator.CreateInstance(catTypeJet);
                    try
                    {
                        var connStrJet = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={Path.ChangeExtension(path, ".mdb")};";
                        catTypeJet.InvokeMember("Create", System.Reflection.BindingFlags.InvokeMethod, null, catJet, new object[] { connStrJet });
                        Log("Se creó un archivo .mdb como alternativa.");
                        return "Microsoft.Jet.OLEDB.4.0";
                    }
                    finally { if (catJet != null) Marshal.ReleaseComObject(catJet); }
                }
            }
            catch (Exception ex)
            {
                Log($"Creación alternativa .mdb falló: {ex.Message}");
            }

            throw new InvalidOperationException("No se pudo crear la base de datos .accdb/.mdb. Instale Microsoft Access o Microsoft Access Database Engine (ACE) y asegure que la arquitectura (x86/x64) coincida con la aplicación.");
        }

        private string DetectProviderForOpen(string path)
        {
            var providers = new[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0", "Microsoft.Jet.OLEDB.4.0" };
            foreach (var prov in providers)
            {
                var connStr = $"Provider={prov};Data Source={path};Persist Security Info=False;";
                try
                {
                    using (var conn = new OleDbConnection(connStr))
                    {
                        conn.Open();
                        conn.Close();
                        return prov;
                    }
                }
                catch (Exception ex)
                {
                    Log($"Intento de apertura con {prov} falló: {ex.Message}");
                }
            }
            throw new InvalidOperationException("No se encontró un proveedor OLEDB registrado que pueda abrir la base de datos. Instale ACE o ajuste la plataforma (x86/x64).");
        }

        private void EnsureTables(OleDbConnection conn)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "IF NOT EXISTS (SELECT * FROM MSysObjects WHERE Name='Categorias') SELECT 1";
            try
            {
                cmd.CommandText = "CREATE TABLE Categorias (Id INTEGER PRIMARY KEY, Nombre TEXT(100))";
                cmd.ExecuteNonQuery();
                Log("Tabla 'Categorias' creada.");
            }
            catch (OleDbException)
            {
                Log("Tabla 'Categorias' ya existe o no pudo crearse (se omitió). ");
            }

            try
            {
                cmd.CommandText = "CREATE TABLE Articulos (Id INTEGER PRIMARY KEY, Nombre TEXT(200), CategoriaId INTEGER, Precio DOUBLE)";
                cmd.ExecuteNonQuery();
                Log("Tabla 'Articulos' creada.");
            }
            catch (OleDbException)
            {
                Log("Tabla 'Articulos' ya existe o no pudo crearse (se omitió). ");
            }
        }

        private void ImportCategorias(OleDbConnection conn)
        {
            var path = Path.Combine(dataFolder, categoriasFileName);
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                if (parts.Length < 2)
                {
                    Log($"Linea de categoria inválida: {line}");
                    continue;
                }
                var id = parts[0].Trim();
                var nombre = parts[1].Trim();
                try
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO Categorias (Id, Nombre) VALUES (?, ?)";
                    cmd.Parameters.AddWithValue("@Id", int.Parse(id));
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.ExecuteNonQuery();
                    Log($"Categoria importada: {id} - {nombre}");
                }
                catch (Exception ex)
                {
                    Log($"Error al insertar categoria {id}: {ex.Message}");
                }
            }
        }

        private void ImportArticulos(OleDbConnection conn)
        {
            var path = Path.Combine(dataFolder, articulosFileName);
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                if (parts.Length < 4)
                {
                    Log($"Linea de articulo inválida: {line}");
                    continue;
                }
                try
                {
                    var id = int.Parse(parts[0].Trim());
                    var nombre = parts[1].Trim();
                    var categoriaId = int.Parse(parts[2].Trim());
                    var precio = double.Parse(parts[3].Trim());

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "INSERT INTO Articulos (Id, Nombre, CategoriaId, Precio) VALUES (?, ?, ?, ?)";
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Nombre", nombre);
                    cmd.Parameters.AddWithValue("@CategoriaId", categoriaId);
                    cmd.Parameters.AddWithValue("@Precio", precio);
                    cmd.ExecuteNonQuery();
                    Log($"Articulo importado: {id} - {nombre}");
                }
                catch (Exception ex)
                {
                    Log($"Error al insertar articulo en linea '{line}': {ex.Message}");
                }
            }
        }
    }
}
