using System;
using System.Configuration;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using ADOX;

namespace pryZarateSP2._2.BaseDeDatos
{
    public static class BaseDatos
    {
        private static string GetConnectionString() // Función privada compartida que devuelve un texto.
        {
            return ConfigurationManager.ConnectionStrings["DistribuidoraDB"].ConnectionString;
            // Devuelve la dirección de conexión llamada "DistribuidoraDB" que está en App.config.
        }

        public static void EnsureCreated() // Función que no devuelve nada.
        {
            var projectDir = GetProjectDirectory(); // Guarda la carpeta del proyecto en una variable.
            AppDomain.CurrentDomain.SetData("DataDirectory", projectDir); // Decile a la app: cuando uses la palabra "DataDirectory", se refiere a la carpeta del proyecto.
            var dbFolder = Path.Combine(projectDir, "Base de Datos"); // Arma la ruta: carpeta del proyecto + "Base de Datos".
            var dbPath = Path.Combine(dbFolder, "Distribuidora.accdb"); // Arma la ruta: carpeta de antes + "Distribuidora.accdb".

            if (!Directory.Exists(dbFolder))
                Directory.CreateDirectory(dbFolder);
            // Si la carpeta "Base de Datos" no existe, creala.

            if (!File.Exists(dbPath))
            {
                CreateDatabaseFile(dbPath);
                CreateTables();
                // Si el archivo .accdb no existe: crealo y crea las tablas.
            }
        }

        private static string GetProjectDirectory() // Función que devuelve un texto.
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); // Empieza en la carpeta donde está corriendo el .exe (bin/Debug).
            while (dir != null) // Mientras haya una carpeta donde mirar, seguí.
            {
                if (dir.GetFiles("*.csproj").Length > 0)
                    return dir.FullName;
                // Si esta carpeta tiene un archivo .csproj, entonces es porque se encontró la del proyecto, la devuelvo.
                dir = dir.Parent;
                // Sino, subí una carpeta más arriba y volvé a probar.
            }
            return AppDomain.CurrentDomain.BaseDirectory;
            // Si nunca encontré un .csproj, devolvé la carpeta del .exe como plan B.
        }

        private static void CreateDatabaseFile(string path) // Función privada que recibe la ruta y crea el archivo Access vacío.
        {
            var catalog = new Catalog(); // Creo un objeto Catalog (de la librería ADOX) que sabe crear archivos Access.
            var connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";"; // Armo el texto de conexión apuntando a la ruta donde quiero crear el archivo.
            catalog.Create(connectionString); // Le digo al catálogo: creá el archivo en esa ruta.
        }

        private static void CreateTables() // Función que crea las dos tablas.
        {
            EjecutarComando(
                "CREATE TABLE Categorias (" +
                "IdCategoria INTEGER NOT NULL PRIMARY KEY, " +
                "Nombre TEXT(100) NOT NULL" +
                ")");

            EjecutarComando(
                "CREATE TABLE Articulos (" +
                "IdArticulo INTEGER NOT NULL PRIMARY KEY, " +
                "Nombre TEXT(150) NOT NULL, " +
                "IdCategoria INTEGER NOT NULL, " +
                "Precio DOUBLE NOT NULL" +
                ")");
        }

        private static void EjecutarComando(string sql, OleDbParameter[] parameters = null) // Recibe un SQL y opcionalmente parámetros.
        {
            using (var conn = new OleDbConnection(GetConnectionString())) // Creo una conexión, se cierra sola al terminar.
            using (var cmd = new OleDbCommand(sql, conn)) // Creo un comando con el SQL y la conexión, se cierra solo al terminar.
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters); // Si me pasaron parámetros, agregalos al comando.
                conn.Open();
                cmd.ExecuteNonQuery(); // Ejecutá el comando.
            }
        }

        public static int MigrarCategorias(string rutaArchivo) //  Recibe la ruta de un archivo y devuelve cuántas categorías agregó.
        { 
            int agregados = 0; 
            var lineas = File.ReadAllLines(rutaArchivo); // Leo todas las líneas del archivo y las guardo en un arreglo.

            foreach (var linea in lineas) // Para cada línea en lineas
            {
                if (string.IsNullOrWhiteSpace(linea)) continue; // Si la línea está vacía, saltala.

                var partes = linea.Split('|');     // Separo la línea por el carácter |. Queda un arreglo con dos partes.
                if (partes.Length < 2) continue;     // Si no hay 2 partes, la línea está mal, saltala.

                int id = int.Parse(partes[0].Trim());     // Convierto la primera parte a número entero.
                string nombre = partes[1].Trim();     // Guardo la segunda parte como texto, sin espacios al principio/final.

                EjecutarComando(
                    "INSERT INTO Categorias (IdCategoria, Nombre) VALUES (?, ?)",
                    new[] {
                        new OleDbParameter("@IdCategoria", id),
                        new OleDbParameter("@Nombre", nombre)
                    });
                // Ejecuto un INSERT pasando el id y el nombre como parámetros.
                agregados++;
            }

            return agregados;
            // Al terminar el bucle, devuelvo cuántas categorías agregué.   
        }

        public static int MigrarArticulos(string rutaArchivo) // Recibe la ruta de un archivo y devuelve cuántos artículos agregó.
        {
            int agregados = 0;
            var lineas = File.ReadAllLines(rutaArchivo); // Leo todas las líneas.

            foreach (var linea in lineas) // Para cada línea...
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;     // Si está vacía, saltala.

                var partes = linea.Split('|');
                if (partes.Length < 4) continue;     // Si no hay 4 partes, la línea está mal.

                int id = int.Parse(partes[0].Trim());     // Convierto el primer valor a entero (IdArticulo).
                string nombre = partes[1].Trim();     // El segundo es el nombre.
                int idCategoria = int.Parse(partes[2].Trim());     // El tercero a entero (IdCategoria).
                double precio = double.Parse(partes[3].Trim(), CultureInfo.InvariantCulture);     // El cuarto a decimal (Precio). Uso InvariantCulture para que entienda el punto como separador decimal.

                EjecutarComando(
                    "INSERT INTO Articulos (IdArticulo, Nombre, IdCategoria, Precio) VALUES (?, ?, ?, ?)",
                    new[] {
                        new OleDbParameter("@IdArticulo", id),
                        new OleDbParameter("@Nombre", nombre),
                        new OleDbParameter("@IdCategoria", idCategoria),
                        new OleDbParameter("@Precio", precio)
                    });
                // Ejecuto un INSERT pasando los 4 valores como parámetros.
                agregados++;
            }

            return agregados;
        }

        public static bool TablaTieneDatos(string nombreTabla) // Recibe el nombre de una tabla y devuelve true/false.

        {
            using (var conn = new OleDbConnection(GetConnectionString())) // Creo una conexión.
            using (var cmd = new OleDbCommand($"SELECT COUNT(*) FROM {nombreTabla}", conn)) // Creo un comando que cuenta cuántas filas hay en esa tabla.
            {
                conn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar()); // Ejecuto el comando y guardo el resultado como número entero.
                return count > 0; // Devuelvo true si hay 1 o más filas, false si está vacía.
            }
        }
    }
}
