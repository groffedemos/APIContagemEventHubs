using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using APIContagem.Models;

namespace APIContagem.EventHubs
{
    public class ContagemProducer
    {
        private readonly ILogger<ContagemProducer> _logger;
        private readonly IConfiguration _configuration;

        public ContagemProducer(
            ILogger<ContagemProducer> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendAsync(ResultadoContador resultadoContador)
        {
            EventHubProducerClient producerClient = null;
            try
            {
                producerClient = new EventHubProducerClient(
                    _configuration["AzureEventHubs:ConnectionString"],
                    _configuration["AzureEventHubs:EventHub"]);
                
                using var eventBatch = await producerClient.CreateBatchAsync();
                _logger.LogInformation("Gerando o Batch para envio dos eventos...");

                var eventData = JsonSerializer.Serialize(resultadoContador,
                    new () { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                _logger.LogInformation($"Evento = {eventData}");
                if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(eventData))))
                    throw new Exception($"O tamanho em dados do evento "+
                        "é superior ao limite suportado e nao será enviado!");

                await producerClient.SendAsync(eventBatch);
                _logger.LogInformation("Concluido o envio dos eventos!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exceção: {ex.GetType().FullName} | " +
                                 $"Mensagem: {ex.Message}");
            }
            finally
            {
                if (producerClient is not null)
                {
                    await producerClient.DisposeAsync();
                    _logger.LogInformation(
                        "Conexao com o Azure Event Hubs finalizada!");
                }
            }
        }       
    }
}