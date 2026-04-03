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
        // UI controls declared in designer but referenced here
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnMigrar;

        private readonly string dataFolder = Path.Combine(Application.StartupPath, "Data");
        private readonly string categoriasFileName = "Categorias.txt";
        private readonly string articulosFileName = "Articulos.txt";
        private readonly string databaseFileName = "Distribuidora.mdb";

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
                if (!File.Exists(dbPath))
                {
                    Log("Base de datos no encontrada. Creando... ");
                    CreateAccessDatabase(dbPath);
                    Log("Base de datos creada.");
                }

                var connString = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={dbPath};";
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

        private void CreateAccessDatabase(string path)
        {
            // Use ADOX Catalog COM object to create an Access .mdb file (late-bound)
            var progId = "ADOX.Catalog";
            var catType = Type.GetTypeFromProgID(progId);
            if (catType == null)
                throw new InvalidOperationException("No se puede obtener ADOX.Catalog. Asegure que MDAC/ADOX estén instalados en el sistema.");

            object cat = null;
            try
            {
                cat = Activator.CreateInstance(catType);
                // call Create method: cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=path;")
                var connStr = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={path};";
                catType.InvokeMember("Create", System.Reflection.BindingFlags.InvokeMethod, null, cat, new object[] { connStr });
            }
            finally
            {
                if (cat != null)
                    Marshal.ReleaseComObject(cat);
            }
        }

        private void EnsureTables(OleDbConnection conn)
        {
            // Create tables if they don't exist
            var cmd = conn.CreateCommand();
            // Categorias: Id (int primary key), Nombre (text)
            cmd.CommandText = "IF NOT EXISTS (SELECT * FROM MSysObjects WHERE Name='Categorias') SELECT 1";
            try
            {
                // Jet SQL doesn't support IF NOT EXISTS in that form; attempt create and ignore errors
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
