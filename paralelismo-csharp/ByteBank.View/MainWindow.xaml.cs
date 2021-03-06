using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using ByteBank.View.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private void BtnCancelar_click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;
            _cts.Cancel(); // reposavel por notificar o token e todos os lugares q utilizam o token vao saber
           

        }

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e) // codigo assincrono
        {
            BtnProcessar.IsEnabled = false; // no inicio do processamento desativa o btn

            _cts = new CancellationTokenSource(); // instancia um novo cancelatino token ao ter uma acao no btn

            var contas = r_Repositorio.GetContaClientes();

            PgsProgresso.Maximum = contas.Count();

            LimparView();

            var inicio = DateTime.Now;

            BtnCancelar.IsEnabled = true; //ativa o botao d cancelar pos processamento

            var progress = new Progress<String>(str => PgsProgresso.Value++); // progress implementado pela MS
            //var byteBankProgress = new ByteBankProgress<String>(str => PgsProgresso.Value ++);

            try
            {//colocamos o try aqui pq foi identificado uma excecao sendo lancada inicialmente aqui
                var resultado = await ConsolidarContas(contas, progress, _cts.Token); //mudando pra await a gente esta retornando uma lista de string e nao uma task | estamos aguardando uma tarefa no contexto da thread inicial e tbm nao nos preocupamos mais com aquele codigo encadeado
                var fim = DateTime.Now;
                AtualizarView(resultado, fim - inicio);
                BtnProcessar.IsEnabled = true;
            }
            catch (OperationCanceledException)
            {
                TxtTempo.Text = "Operacao cancelada"; 
            }
            finally
            {
                BtnProcessar.IsEnabled = true;
                BtnCancelar.IsEnabled = false;
            }

        }

        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas, IProgress<string> reportadorDeProgresso, CancellationToken ct)
        {
            var contasProcessadas = contas.Select(conta =>
                Task.Factory.StartNew(() =>
                {
                    // if (ct.IsCancellationRequested) // convercao colocar antes e 
                    // {
                    //     throw new OperationCanceledException(ct);
                    //   }

                    ct.ThrowIfCancellationRequested(); //mesma coisa do if acima
                    var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta);
                    reportadorDeProgresso.Report(resultadoConsolidacao);
                    //if (ct.IsCancellationRequested) // depois a opcao de cancelar
                    //{
                    //   throw new OperationCanceledException(ct);
                    //}
                    ct.ThrowIfCancellationRequested();

                    return resultadoConsolidacao;
                },ct) // passando o cancelationtoken dessa maneira como segundo parametro o task scheduller verifica antes de iniciar todo o prcoessamento que a tarefa ja foi cancelada
             );
               
            return await Task.WhenAll(contasProcessadas); //retorna um array de string
        }


        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
            PgsProgresso.Value = 0;
        }

        private void AtualizarView(IEnumerable<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }

        // var contasTarefas = contas.Select(conta => //
        // {
        //    return Task.Factory.StartNew(() => //o factory demanda as tarefas para as taskScheduller, ele gerencia!
        //    {
        //        var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
        //        resultado.Add(resultadoProcessamento);
        //   });
        //}).ToArray(); // o to Array é necessario para "obrigar" o select(linq) a efetuar o processo para todas as contas

        //Task.WaitAll(contasTarefas); // espera o processamento de todas as tarefas(contas a ser processadas) serem concluidas MAS temos o problema da interface ficar congelada

        // -=-=-= ISSO FOI SUBSTITUIDO PELO ASSYNC AWAIT, O COMPILADOR JA AUTOMATIZA TODO ESSE PROCESSO MANUAL -=-=-=-=
        // ConsolidarContas(contas) // resolvendo o problema de congelar a UI pois o whenAll executa apos finalizar uma tarefa
        //     .ContinueWith(task =>
        //   {
        //      var fim = DateTime.Now;
        //     var resultados = task.Result;
        //    AtualizarView(resultados, fim - inicio);
        //}, taskSchedullerUi) // executa de acordo com a demanda taskSchedullerUi definido na linha 2 e resolve o erro da excecao de threadh nao permitida pois o InvalidOperationException, ocorre quando a tentativa de se acessar um objeto da Thread da interface gráfica a partir de outra Thread
        //.ContinueWith(task =>
        // {
        //   BtnProcessar.IsEnabled = true; //ativa o btn apos o processamento
        //});


        /* codigo antigo - THREADS

        neste momento nao estamos trabalhando com o gerenciador de tarefas, estamos definindo individualmente
        as quantidade de tarefas por treahds

        var contasQuantidadePorThread = contas.Count() / 4; // como vou usar 4 threahds vou dividir em porcao de 4
        var contas_parte1 = contas.Take(contasQuantidadePorThread);
        var contas_parte2 = contas.Skip(contasQuantidadePorThread).Take(contasQuantidadePorThread);
        var contas_parte3 = contas.Skip(contasQuantidadePorThread*2).Take(contasQuantidadePorThread);
        var contas_parte4 = contas.Skip(contasQuantidadePorThread * 3);
        //var contas_parte1 = contas.Take(contas.Count() / 2); //pega a primeira metade da lista
        //var contas_parte2 = contas.Skip(contas.Count() / 2); // pula pra metade e pega essa metade

        Thread thread_parte1 = new Thread(() => //processa a primeira metade da lista
        {
            foreach (var conta in contas_parte1)
            {
                var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                resultado.Add(resultadoProcessamento);
            }
        });

        Thread thread_parte2 = new Thread(() => //processa a segunda metade da lista
        {
            foreach (var conta in contas_parte2)
            {
                var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                resultado.Add(resultadoProcessamento);
            }
        });

        Thread thread_parte3 = new Thread(() => //processa a segunda metade da lista
        {
            foreach (var conta in contas_parte3)
            {
                var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                resultado.Add(resultadoProcessamento);
            }
        });

        Thread thread_parte4 = new Thread(() => //processa a segunda metade da lista
        {
            foreach (var conta in contas_parte4)
            {
                var resultadoProcessamento = r_Servico.ConsolidarMovimentacao(conta);
                resultado.Add(resultadoProcessamento);
            }
        });

        thread_parte1.Start(); // inicia o processamento da thread
        thread_parte2.Start();
        thread_parte3.Start();
        thread_parte4.Start();
        while (thread_parte1.IsAlive || thread_parte2.IsAlive || thread_parte3.IsAlive || thread_parte4.IsAlive) // verifica se as thread estao em execucao (isAlive)
        {
            Thread.Sleep(250); // ao inves de perguntar SEMPRE vamos perguntar uma vez e esperar o tempo definido com isso diminuimos 20s do processamento
        }
         */

    }
}

/*
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
Alteramos uma aplicação de cálculos matemáticos na execução de algumas funções paralelas a fim de tornar a aplicação responsiva!
Com a implementação da função FatorialEmParalelo(int) abaixo, obteremos qual comportamento ao chamar var a = FatorialEmParalelo(5)?
 
 * public static long FatorialEmParalelo(int valor) 
    { 
        long resultado = 0; 

        var t_calculo = new Thread(() => resultado = Fatorial(valor)); 
        t_calculo.Start(); 

        return resultado; 
    } 

private static long Fatorial(int valor)
       {
           var resultado = 1L;
           for (int i = valor; i >= 2; i--)
               resultado *= i;
           return resultado;
       }

R: Correto. A função FatorialEmParalelo retornará 0, pois ela não aguarda o término da thread t_calculo antes de retornar o valor da variável resultado, que se mantém 0
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

Você deseja criar uma Thread que não faz nada por 5 segundos. Qual a diferença dos códigos a seguir?

1.
var inicio = DateTime.Now;
while(true) {
     if (DateTime.Now - inicio >= TimeSpan.FromSeconds(5))
         break;
}

2.
Thread.Sleep(5000);

R: O código em 2. faz menor uso de recursos da CPU, indicando ao sistema operacional que aquela Thread não deve ser executada nos próximos 5 segundos
Correta. O método Thread.Sleep(int) indica ao sistema operacional que aquela Thread não deve ser executada na CPU pelos próximos 5000 milissegundos, dando prioridade para outras Threads da aplicação ou outros programas a usarem a CPU da máquina. Enquanto que no código 1.
temos um uso intensivo da CPU para o cálculo do período de tempo passado entre uma verificação e outra
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

Considere o código a seguir:

var thread = new Thread(() => Thread.Sleep(5000));
var isAlive = thread.IsAlive;
Console.WriteLine(isAlive);

Qual será o valor booleano de isAlive?
R: False. Dado que a thread não foi iniciada com o método Start, sua propriedade IsAlive nunca será true
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

Do ponto de vista de performance, em um processador quad-core, qual a diferença entre os códigos abaixo?

1.
for(int i = 0; i < 100; i++) {
    var msg = "Thread número " + i;
    var thread = new Thread(() => Console.WriteLine(msg));
        thread.Start();
}

2.
for(int i = 0; i < 100; i++) {
    var msg = "Task número " + i;
    Task.Factory.StartNew(() => Console.WriteLine(msg));
}

A solução em 2. é mais performática, porque passamos a responsabilidade de provisionamento e gerenciamento das Threads disponíveis para o TaskScheduler default usado pela Task.Factory, que possui uma inteligência
O uso da Task.Factory e o default TaskScheduler é uma boa prática pois o gerenciamento das threads por eles é feito de forma muito inteligente
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

Você é usuário de uma biblioteca que possui um método com a assinatura Task IniciarDownload() e quer executar um método chamado FimDownload() ao término dela, mantendo a UI responsiva. O código atual é:

var download = new Download(url);
download.IniciarDownload();

Qual alternativa reflete a correta implementação para atender este requisito?

download.IniciarDownload().ContinueWith(() => FimDownload());


Correta. ContinueWith é o método correto que nos permite encadear a execução de uma tarefa em outra
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

No código var A = CalculaRaiz(100); qual será o tipo da variável A?

private async Task<double> CalculaRaiz(double num)
{
    return await Task.Run(() => Math.Sqrt(num));
}

Task<double>, pois é, de fato, o tipo retornado por CalculaRaiz e na atribuição não há o uso da palavra chave await
Correta. Como não foi feito uso da palavra chave await na atribuição da variável A, o compilador não interpreta que estamos interessados no valor de retorno da tarefa e sim no tipo Task<double>
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

O que o compilador faz no código abaixo?

01 | btnCalcular.IsEnabled = false;
02 | var A = await CalculaRaiz(100);
03 | btnCalcular.IsEnabled = true;

O compilador reescreve o código de forma que a linha 2 seja executada em uma Task e a linha 3 seja executada de forma encadeada, mas no mesmo contexto da linha 1
Correta. Esta é uma das grandes motivações do async/await. O contexto da linha 1 é preservado e mantido na execução da linha 3

-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
Por que não precisamos nos preocupar com contexto de sincronização ao usarmos a classe Progress<T>

Porque um objeto do tipo Progress<T> sempre captura o contexto de sincronização do TaskScheduler da thread que criou sua instância
Correta. O objeto Progress<T> captura o contexto da thread que criou sua instância


-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

Em que momento uma exceção do tipo OperationCanceledException é lançada?
No momento em que o método ThrowIfCancellationRequested de um objeto do tipo CancellationToken é invocado
Correta. O método ThrowIfCancellationRequested sempre lança uma exceção quando há uma solicitação de cancelamento

Ao lançarmos manualmente, após verificarmos que a propriedade IsCancellationRequested é verdadeira
Correta. É uma prática comum lançar a exceção manualmente, após verificar-se a propriedade IsCancellationRequested, pois ainda podemos reverter alguma ação, caso necessário
*/