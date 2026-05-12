using System;
using System.IO;
using System.Windows.Forms;
using pryZarateSP2._2.BaseDeDatos;

namespace pryZarateSP2._2
{
    public partial class frmPrincipal : Form
    {
        private readonly string archivosFolder; // Variable privada de solo lectura, guarda la ruta de la carpeta "Archivos".
        private readonly string categoriasPath; // Variable privada de solo lectura, guarda la ruta del archivo Categorias.txt.
        private readonly string articulosPath; // Variable privada de solo lectura, guarda la ruta del archivo Articulos.txt.

        public frmPrincipal()
        {
            InitializeComponent();

            var projectDir = (string)AppDomain.CurrentDomain.GetData("DataDirectory");// Tomo la ruta del proyecto que había guardado antes en EnsureCreated y la convierto a texto.
            archivosFolder = Path.Combine(projectDir, "Archivos"); // Armo la ruta de la carpeta Archivos: proyecto + "Archivos".
            categoriasPath = Path.Combine(archivosFolder, "Categorias.txt"); // Armo la ruta del archivo Categorias.txt.
            articulosPath = Path.Combine(archivosFolder, "Articulos.txt"); // Armo la ruta del archivo Articulos.txt.
        }

        private void Log(string mensaje) // Recibe un texto.
        {
            txtLog.AppendText(mensaje + Environment.NewLine); // Agrega el mensaje al textbox + un salto de línea al final.
        }

        private void btnMigrar_Click(object sender, EventArgs e) //Se ejecuta al hacer clic en el botón Migrar.

        {
            txtLog.Clear(); // Limpio el textbox.
            btnMigrar.Enabled = false; // Deshabilito el botón mientras se ejecuta la migración (para que no lo aprieten dos veces).

            try // Intento ejecutar todo lo que sigue. Si algo falla, salta al catch.
            {
                if (!File.Exists(categoriasPath))
                {
                    MessageBox.Show("No se encontro el archivo Categorias.txt en la carpeta Archivos.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Si NO existe Categorias.txt, muestro cartel de error y salgo del método.

                if (!File.Exists(articulosPath))
                {
                    MessageBox.Show("No se encontro el archivo Articulos.txt en la carpeta Archivos.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Si NO existe Articulos.txt, muestro cartel de error y salgo.

                if (BaseDatos.TablaTieneDatos("Categorias") || BaseDatos.TablaTieneDatos("Articulos"))
                {
                    // Si alguna de las dos tablas ya tiene datos...
                    var resp = MessageBox.Show(
                        "La base ya contiene datos. Si continua, pueden generarse errores por IDs duplicados.\n\n¿Desea continuar igual?",
                        "Atencion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    // ...muestro un cartel preguntando si quiere seguir.

                    if (resp != DialogResult.Yes) return;
                    // Si NO dijo "Sí", salgo del método.
                }

                Log("Migrando datos de Categorias...");     // Aviso en el log.
                int catAgregadas = BaseDatos.MigrarCategorias(categoriasPath);
                // Llamo a BaseDatos.MigrarCategorias pasándole la ruta del archivo. Me devuelve cuántas categorías agregó.
                Log($"Se incorporaron: {catAgregadas} registros nuevos.");
                // Muestro en el log cuántos registros se agregaron.
                Log("");

                Log("Migrando datos de Articulos...");
                // Aviso.
                int artAgregados = BaseDatos.MigrarArticulos(articulosPath);
                // Llamo a BaseDatos.MigrarArticulos y guardo cuántos artículos agregó.
                Log($"Se incorporaron: {artAgregados} registros nuevos.");
                // Muestro cuántos artículos se agregaron.
                Log("");

                Log("Migracion finalizada.");
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
                // Si algo falló, escribo el mensaje de error en el log.
                MessageBox.Show("Ocurrio un error durante la migracion:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        // Y muestro también un cartel con el error.
            }
            finally
            {
                btnMigrar.Enabled = true;
            }
        }

        private void frmPrincipal_Load(object sender, EventArgs e)
        {

        }
    }
}
