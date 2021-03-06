using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Alura.LeilaoOnline.WebApp.Dados;
using Alura.LeilaoOnline.WebApp.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alura.LeilaoOnline.WebApp.Controllers
{
    public class LeilaoController : Controller
    {

        AppDbContext _context;

        public LeilaoController()
        {
            _context = new AppDbContext();
        }

        public IActionResult Index()
        {
            return View(BuscarLeilao());
        }

        private IEnumerable<Leilao> BuscarLeilao()
        {
            return _context.Leiloes
                .Include(l => l.Categoria);
        }

        private IEnumerable<Categoria> BuscarCategoria()
        {
            return _context.Categorias.ToList();
        }

        private Leilao BuscarLeilaoByID(int id)
        {
            return _context.Leiloes.Find(id);
        }

        [HttpGet]
        public IActionResult Insert()
        {
            ViewData["Categorias"] = BuscarCategoria();
            ViewData["Operacao"] = "Inclusão"; //code smell
            return View("Form");
        }

        [HttpPost]
        public IActionResult Insert(Leilao model)
        {
            if (ModelState.IsValid)
            {
                _context.Leiloes.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewData["Categorias"] = BuscarCategoria();
            ViewData["Operacao"] = "Inclusão"; //code smell
            return View("Form", model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewData["Categorias"] = BuscarCategoria();
            ViewData["Operacao"] = "Edição"; //code smell
            var leilao = _context.Leiloes.Find(id);
            if (leilao == null) return NotFound();
            return View("Form", leilao);
        }

        [HttpPost]
        public IActionResult Edit(Leilao model)
        {
            if (ModelState.IsValid)
            {
                _context.Leiloes.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewData["Categorias"] = _context.Categorias.ToList(); //code smell
            ViewData["Operacao"] = "Edição"; //code smell
            return View("Form", model);
        }

        [HttpPost]
        public IActionResult Inicia(int id)
        {
            var leilao = BuscarLeilaoByID(id);
            if (leilao == null) return NotFound(); //code smell
            if (leilao.Situacao != SituacaoLeilao.Rascunho) return StatusCode(405); //code smell
            leilao.Situacao = SituacaoLeilao.Pregao;
            leilao.Inicio = DateTime.Now;
            _context.Leiloes.Update(leilao);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Finaliza(int id)
        {
            var leilao = BuscarLeilaoByID(id);
            if (leilao == null) return NotFound(); //code smell
            if (leilao.Situacao != SituacaoLeilao.Pregao) return StatusCode(405); 
            leilao.Situacao = SituacaoLeilao.Finalizado;
            leilao.Termino = DateTime.Now;
            _context.Leiloes.Update(leilao);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int id)
        {
            var leilao = BuscarLeilaoByID(id);
            if (leilao == null) return NotFound(); //code smell
            if (leilao.Situacao == SituacaoLeilao.Pregao) return StatusCode(405); //code smell
            _context.Leiloes.Remove(leilao);
            _context.SaveChanges();
            return NoContent();
        }

        [HttpGet]
        public IActionResult Pesquisa(string termo)
        {
            ViewData["termo"] = termo;
            var leiloes = _context.Leiloes
                .Include(l => l.Categoria)
                .Where(l => string.IsNullOrWhiteSpace(termo) || 
                    l.Titulo.ToUpper().Contains(termo.ToUpper()) || 
                    l.Descricao.ToUpper().Contains(termo.ToUpper()) ||
                    l.Categoria.Descricao.ToUpper().Contains(termo.ToUpper())
                );
            return View("Index", leiloes);
        }
    }
}

// ESTAMOS BUSCANDO POR CODE SMELL (CODIGOS REPETIDOS NESTE CASO)