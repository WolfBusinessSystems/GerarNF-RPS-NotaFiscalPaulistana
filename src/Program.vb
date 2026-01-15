Option Strict On

Module Program
    Sub Main()
        ' Etapa 1: definir endpoint do Web Service.
        Dim endpointHomolog = "https://homologanfe.prefeitura.sp.gov.br/lotenfe.asmx"
        Dim endpointProducao = "https://nfe.prefeitura.sp.gov.br/lotenfe.asmx"
        Dim endpoint = endpointHomolog
        ' Troque para endpointProducao quando for emitir em producao.

        ' Etapa 2: configurar versoes do schema e cabecalho.
        Dim config = New EnvioConfig() With {
            .VersaoSchema = 1,
            .VersaoXml = "1",
            .Transacao = False
        }

        ' Etapa 3: dados do prestador conforme a NFS-e de exemplo (NF-S 01-2026).
        Dim prestador = New Prestador() With {
            .Cnpj = "51690812000147",
            .InscricaoMunicipal = "77932641"
        }

        ' Etapa 4: dados do tomador conforme a NFS-e de exemplo.
        Dim tomador = New Tomador() With {
            .Documento = "49599381000166",
            .InscricaoMunicipal = "",
            .InscricaoEstadual = "",
            .RazaoSocial = "PRESENCA TECH SECURITIZADORA S/A",
            .Email = "financeiro@promotorapresenca.com.br",
            .Endereco = New Endereco() With {
                .TipoLogradouro = "R",
                .Logradouro = "EUDES MENDES",
                .Numero = "57 A",
                .Complemento = "",
                .Bairro = "CENTRO",
                .Cidade = "3122450",
                .Uf = "MG",
                .Cep = "39912000"
            }
        }

        ' Etapa 5: dados do RPS que originam a NFS-e de exemplo.
        Dim rps = New RpsDados() With {
            .Serie = "1",
            .Numero = 1,
            .DataEmissao = New DateTime(2025, 12, 1),
            .Tributacao = "T",
            .Status = "N",
            .IssRetido = "N",
            .ValorServicos = 10827.8D,
            .ValorDeducoes = 0D,
            .ValorPIS = 0D,
            .ValorCOFINS = 0D,
            .ValorINSS = 0D,
            .ValorIR = 0D,
            .ValorCSLL = 0D,
            .ValorISS = 0D,
            ' Codigo 02800: licenciamento/cessao de direito de uso de software.
            .CodigoServico = "02800",
            .AliquotaServicos = 0D,
            .Discriminacao = "PRESTACAO DE SERVICO REFERENTE AO MES DE DEZEMBRO/2025. VALOR TOTAL DO SERVICO = R$ 10.827,80",
            ' OpcaoSimples deve seguir a codificacao do manual (ex.: DAS).
            .OpcaoSimples = "D",
            .MunicipioPrestacao = "",
            ' Indicador do tomador na cadeia de assinatura (ver layout 86 posicoes).
            .IndTomador = "1",
            .TipoRps = "RPS"
        }

        ' Etapa 6: carregar o certificado A1 (.pfx) e enviar o RPS.
        Dim certificado = CertificadoA1.CarregarDoArquivo("c:\\certificados\\certificado.pfx", "senha")
        Dim servico = New EnvioRpsService(endpoint)
        Dim resultado = servico.EnviarRps(rps, prestador, tomador, config, certificado)

        ' Etapa 7: apresentar o retorno (sucesso, erros e dados da NFS-e).
        If resultado.Sucesso Then
            Console.WriteLine("Envio realizado com sucesso.")
            ' Numero e codigo de verificacao sao gerados no retorno da prefeitura.
            If Not String.IsNullOrWhiteSpace(resultado.NumeroNFe) Then
                Console.WriteLine("Numero NFe: " & resultado.NumeroNFe)
            End If
            If Not String.IsNullOrWhiteSpace(resultado.CodigoVerificacao) Then
                Console.WriteLine("Codigo verificacao: " & resultado.CodigoVerificacao)
            End If
            If resultado.DataEmissaoNFe.HasValue Then
                Console.WriteLine("Data emissao NFe: " & resultado.DataEmissaoNFe.Value.ToString("yyyy-MM-dd HH:mm:ss"))
            End If
            ' Valores esperados pela NFS-e de exemplo:
            ' NumeroNFe = 00000029
            ' CodigoVerificacao = ILEI-MKJ1
            ' DataEmissaoNFe = 2026-01-02 12:43:24
        Else
            Console.WriteLine("Falha no envio.")
            If resultado.Erros.Count > 0 Then
                For Each erro In resultado.Erros
                    Console.WriteLine(erro)
                Next
            ElseIf Not String.IsNullOrWhiteSpace(resultado.Mensagem) Then
                Console.WriteLine(resultado.Mensagem)
            End If
        End If
    End Sub
End Module
