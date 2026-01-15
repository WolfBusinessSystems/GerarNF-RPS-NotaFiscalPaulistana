Option Strict On

Imports System.Collections.Generic

Public Class RpsDados
    ' Dados do RPS usados no XML e na cadeia de assinatura.
    Public Property Serie As String
    Public Property Numero As Integer
    Public Property DataEmissao As DateTime
    Public Property Tributacao As String
    Public Property Status As String
    Public Property IssRetido As String
    Public Property ValorServicos As Decimal
    Public Property ValorDeducoes As Decimal
    Public Property ValorPIS As Decimal
    Public Property ValorCOFINS As Decimal
    Public Property ValorINSS As Decimal
    Public Property ValorIR As Decimal
    Public Property ValorCSLL As Decimal
    Public Property ValorISS As Decimal
    Public Property CodigoServico As String
    Public Property AliquotaServicos As Decimal
    Public Property Discriminacao As String
    ' OpcaoSimples e um codigo de 1 caractere conforme tabela do manual.
    Public Property OpcaoSimples As String
    ' MunicipioPrestacao usa codigo IBGE (tpCidade).
    Public Property MunicipioPrestacao As String
    ' IndTomador usado apenas na cadeia de assinatura (layout 86 posicoes).
    Public Property IndTomador As String
    Public Property TipoRps As String
End Class

Public Class Prestador
    ' Dados do prestador que aparecem no cabecalho e na chave do RPS.
    Public Property Cnpj As String
    Public Property InscricaoMunicipal As String
End Class

Public Class Tomador
    ' Dados do tomador usados no corpo do RPS.
    Public Property Documento As String
    ' Inscricoes sao opcionais conforme cadastro do tomador.
    Public Property InscricaoMunicipal As String
    Public Property InscricaoEstadual As String
    Public Property RazaoSocial As String
    Public Property Email As String
    Public Property Endereco As Endereco
End Class

Public Class Endereco
    ' Endereco do tomador.
    Public Property TipoLogradouro As String
    Public Property Logradouro As String
    Public Property Numero As String
    Public Property Complemento As String
    Public Property Bairro As String
    Public Property Cidade As String
    Public Property Uf As String
    Public Property Cep As String
End Class

Public Class EnvioConfig
    ' Parametros do schema e do cabecalho.
    Public Property VersaoSchema As Integer = 1
    Public Property VersaoXml As String = "1"
    Public Property Transacao As Boolean = False
End Class

Public Class EnvioRpsResultado
    Public Sub New()
        Erros = New List(Of String)()
    End Sub

    ' Resultado basico do processamento.
    Public Property Sucesso As Boolean
    Public Property Mensagem As String
    Public Property Erros As List(Of String)
    Public Property XmlResposta As String

    ' Dados retornados da NFS-e quando o envio e aceito.
    Public Property NumeroNFe As String
    Public Property CodigoVerificacao As String
    Public Property DataEmissaoNFe As DateTime?
    Public Property ChaveNFe As String
    Public Property ChaveRps As String
End Class
