using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using APIContagem.EventHubs;
using APIContagem.Models;

namespace APIContagem.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContadorController : ControllerBase
    {
        private static readonly Contador _CONTADOR = new Contador();
        private readonly ILogger<ContadorController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ContagemProducer _producerEventHub;

        public ContadorController(ILogger<ContadorController> logger,
            IConfiguration configuration,
            ContagemProducer producerEventHub)
        {
            _logger = logger;
            _configuration = configuration;
            _producerEventHub = producerEventHub;
        }

        [HttpGet]
        public async Task<ResultadoContador> Get()
        {
            int valorAtualContador;
            lock (_CONTADOR)
            {
                _CONTADOR.Incrementar();
                valorAtualContador = _CONTADOR.ValorAtual;
            }

            _logger.LogInformation(
                $"Processando requisicao - Valor atual do Contador = {valorAtualContador}");

            var resultado = new ResultadoContador()
            {
                ValorAtual = valorAtualContador,
                Producer = _CONTADOR.Local,
                Kernel = _CONTADOR.Kernel,
                TargetFramework = _CONTADOR.TargetFramework,
                Mensagem = _configuration["MensagemVariavel"]
            };

            await _producerEventHub.SendAsync(resultado);

            return resultado;
        }
    }
}