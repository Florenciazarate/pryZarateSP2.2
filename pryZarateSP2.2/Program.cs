using System;
using System.Windows.Forms;
using pryZarateSP2._2.BaseDeDatos;

namespace pryZarateSP2._2
{
    internal static class Program
    {
        [STAThread]
        static void Main() // Es el punto de entrada de la app.
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                BaseDatos.EnsureCreated(); // Verifica si existe la base de datos y la crea si no.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al inicializar la base de datos:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new frmPrincipal());
        }
    }
}
