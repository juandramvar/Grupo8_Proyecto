using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;


namespace Grupo8_Proyecto.Controllers
{
    public class UsuarioController : Controller
    {
        private AhorcadoContext db = new AhorcadoContext();

        // GET: Usuario/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Usuario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                // Verificar que la identificación no exista
                var existeUsuario = db.Usuarios.Any(u => u.usu_id == usuario.usu_id);

                if (existeUsuario)
                {
                    ModelState.AddModelError("usu_id", "Ya existe un usuario con esta identificación.");
                    return View(usuario);
                }

                // Inicializar valores por defecto
                usuario.usu_marcador = 0;
                usuario.usu_ganadas = 0;
                usuario.usu_perdidas = 0;

                db.Usuarios.Add(usuario);
                db.SaveChanges();

                TempData["Mensaje"] = "Usuario creado exitosamente.";
                return RedirectToAction("Index", "Home");
            }

            return View(usuario);
        }

        // GET: Usuario/Escalafon
        public ActionResult Escalafon()
        {
            var usuarios = db.Usuarios
                .OrderByDescending(u => u.usu_marcador ?? 0)
                .ToList();

            return View(usuarios);
        }

        // GET: Usuario/GetUsuarios - Para dropdown en crear partida
        public JsonResult GetUsuarios()
        {
            var usuarios = db.Usuarios
                .Select(u => new {
                    Value = u.usu_id,
                    Text = u.usu_id + " - " + u.usu_nombre
                })
                .ToList();

            return Json(usuarios, JsonRequestBehavior.AllowGet);
        }

        // Método para actualizar puntaje después de una partida
        public bool ActualizarPuntaje(int usuarioId, string nivel, bool gano)
        {
            try
            {
                var usuario = db.Usuarios.Find(usuarioId);
                if (usuario == null) return false;

                int puntos = 0;
                switch (nivel.ToLower())
                {
                    case "fácil":
                    case "facil":
                        puntos = 1;
                        break;
                    case "normal":
                        puntos = 2;
                        break;
                    case "difícil":
                    case "dificil":
                        puntos = 3;
                        break;
                    default:
                        return false;
                }

                if (gano)
                {
                    usuario.usu_marcador = (usuario.usu_marcador ?? 0) + puntos;
                    usuario.usu_ganadas = (usuario.usu_ganadas ?? 0) + 1;
                }
                else
                {
                    usuario.usu_marcador = (usuario.usu_marcador ?? 0) - puntos;
                    usuario.usu_perdidas = (usuario.usu_perdidas ?? 0) + 1;
                }

                db.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // GET: Usuario/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }

            Usuario usuario = db.Usuarios.Find(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            return View(usuario);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}