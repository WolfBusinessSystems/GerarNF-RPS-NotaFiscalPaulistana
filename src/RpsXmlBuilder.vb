Option Strict On

Imports System.Globalization
Imports System.Text
Imports System.Xml

Public NotInheritable Class RpsXmlBuilder
    Private Const NamespaceNfe As String = "http://www.prefeitura.sp.gov.br/nfe"

    Private Sub New()
    End Sub

    Public Shared Function MontarPedidoEnvioRpsXml(
        dados As RpsDados,
        prestador As Prestador,
        tomador As Tomador,
        config As EnvioConfig,
        assinaturaRps As String
    ) As XmlDocument
        ' Etapa 1: validar entradas basicas.
        If dados Is Nothing Then
            Throw New ArgumentNullException(NameOf(dados))
        End If
        If prestador Is Nothing Then
            Throw New ArgumentNullException(NameOf(prestador))
        End If
        If tomador Is Nothing Then
            Throw New ArgumentNullException(NameOf(tomador))
        End If
        If config Is Nothing Then
            Throw New ArgumentNullException(NameOf(config))
        End If

        ' Etapa 2: criar documento e raiz no namespace oficial.
        Dim doc = New XmlDocument()
        doc.PreserveWhitespace = True

        Dim root = doc.CreateElement("PedidoEnvioRPS", NamespaceNfe)
        doc.AppendChild(root)

        ' Etapa 3: montar o cabecalho conforme o manual (PedidoEnvioRPS.xsd).
        Dim cabecalho = doc.CreateElement("Cabecalho", NamespaceNfe)
        cabecalho.SetAttribute("Versao", config.VersaoXml)
        cabecalho.AppendChild(CriarNoCpfCnpj(doc, "CPFCNPJRemetente", prestador.Cnpj))
        AppendElement(cabecalho, "InscricaoMunicipalRemetente", NamespaceNfe, SomenteNumeros(prestador.InscricaoMunicipal))
        AppendElement(cabecalho, "transacao", NamespaceNfe, config.Transacao.ToString().ToLowerInvariant())
        AppendElement(cabecalho, "dtInicio", NamespaceNfe, dados.DataEmissao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
        AppendElement(cabecalho, "dtFim", NamespaceNfe, dados.DataEmissao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
        AppendElement(cabecalho, "QtdRPS", NamespaceNfe, "1")
        AppendElement(cabecalho, "ValorTotalServicos", NamespaceNfe, FormatarValorXml(dados.ValorServicos))
        AppendElement(cabecalho, "ValorTotalDeducoes", NamespaceNfe, FormatarValorXml(dados.ValorDeducoes))
        root.AppendChild(cabecalho)

        ' Etapa 4: montar o bloco do RPS com Id para assinatura (quando necessario).
        Dim rps = doc.CreateElement("RPS", NamespaceNfe)
        Dim idRps = ObterIdRps(dados, prestador)
        rps.SetAttribute("Id", idRps)

        ' Etapa 5: inserir a assinatura baseada na cadeia de 86 posicoes.
        AppendElement(rps, "Assinatura", NamespaceNfe, assinaturaRps)

        ' Etapa 6: chave do RPS.
        Dim chaveRps = doc.CreateElement("ChaveRPS", NamespaceNfe)
        AppendElement(chaveRps, "InscricaoMunicipal", NamespaceNfe, SomenteNumeros(prestador.InscricaoMunicipal))
        AppendElement(chaveRps, "SerieRPS", NamespaceNfe, dados.Serie)
        AppendElement(chaveRps, "NumeroRPS", NamespaceNfe, dados.Numero.ToString(CultureInfo.InvariantCulture))
        rps.AppendChild(chaveRps)

        ' Etapa 7: campos principais do RPS (dados do servico).
        AppendElement(rps, "TipoRPS", NamespaceNfe, If(String.IsNullOrWhiteSpace(dados.TipoRps), "RPS", dados.TipoRps))
        AppendElement(rps, "DataEmissao", NamespaceNfe, dados.DataEmissao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
        AppendElement(rps, "StatusRPS", NamespaceNfe, dados.Status)
        AppendElement(rps, "TributacaoRPS", NamespaceNfe, dados.Tributacao)
        AppendElement(rps, "ValorServicos", NamespaceNfe, FormatarValorXml(dados.ValorServicos))
        AppendElement(rps, "ValorDeducoes", NamespaceNfe, FormatarValorXml(dados.ValorDeducoes))
        AppendElement(rps, "ValorPIS", NamespaceNfe, FormatarValorXml(dados.ValorPIS))
        AppendElement(rps, "ValorCOFINS", NamespaceNfe, FormatarValorXml(dados.ValorCOFINS))
        AppendElement(rps, "ValorINSS", NamespaceNfe, FormatarValorXml(dados.ValorINSS))
        AppendElement(rps, "ValorIR", NamespaceNfe, FormatarValorXml(dados.ValorIR))
        AppendElement(rps, "ValorCSLL", NamespaceNfe, FormatarValorXml(dados.ValorCSLL))
        AppendElement(rps, "CodigoServico", NamespaceNfe, SomenteNumeros(dados.CodigoServico))
        AppendElement(rps, "AliquotaServicos", NamespaceNfe, FormatarAliquotaXml(dados.AliquotaServicos))
        AppendElement(rps, "ValorISS", NamespaceNfe, FormatarValorXml(dados.ValorISS))
        AppendElement(rps, "ISSRetido", NamespaceNfe, NormalizarIssRetidoXml(dados.IssRetido))
        AppendElement(rps, "Discriminacao", NamespaceNfe, dados.Discriminacao)
        AppendElementIfNotEmpty(rps, "MunicipioPrestacao", NamespaceNfe, SomenteNumeros(dados.MunicipioPrestacao))
        AppendElementIfNotEmpty(rps, "OpcaoSimples", NamespaceNfe, dados.OpcaoSimples)

        ' Etapa 8: tomador e endereco.
        rps.AppendChild(CriarNoCpfCnpj(doc, "CPFCNPJTomador", tomador.Documento))
        AppendElementIfNotEmpty(rps, "InscricaoMunicipalTomador", NamespaceNfe, SomenteNumeros(tomador.InscricaoMunicipal))
        AppendElementIfNotEmpty(rps, "InscricaoEstadualTomador", NamespaceNfe, tomador.InscricaoEstadual)
        AppendElement(rps, "RazaoSocialTomador", NamespaceNfe, tomador.RazaoSocial)

        If tomador.Endereco IsNot Nothing Then
            Dim endereco = doc.CreateElement("EnderecoTomador", NamespaceNfe)
            AppendElement(endereco, "TipoLogradouro", NamespaceNfe, tomador.Endereco.TipoLogradouro)
            AppendElement(endereco, "Logradouro", NamespaceNfe, tomador.Endereco.Logradouro)
            AppendElement(endereco, "NumeroEndereco", NamespaceNfe, tomador.Endereco.Numero)
            AppendElementIfNotEmpty(endereco, "ComplementoEndereco", NamespaceNfe, tomador.Endereco.Complemento)
            AppendElement(endereco, "Bairro", NamespaceNfe, tomador.Endereco.Bairro)
            AppendElement(endereco, "Cidade", NamespaceNfe, SomenteNumeros(tomador.Endereco.Cidade))
            AppendElement(endereco, "UF", NamespaceNfe, tomador.Endereco.Uf)
            AppendElement(endereco, "CEP", NamespaceNfe, SomenteNumeros(tomador.Endereco.Cep))
            rps.AppendChild(endereco)
        End If

        If Not String.IsNullOrWhiteSpace(tomador.Email) Then
            AppendElement(rps, "EmailTomador", NamespaceNfe, tomador.Email)
        End If

        ' Etapa 9: anexar RPS ao pedido.
        root.AppendChild(rps)

        Return doc
    End Function

    Public Shared Function ObterIdRps(dados As RpsDados, prestador As Prestador) As String
        ' Id usado para assinatura alternativa por RPS (quando exigido pelo schema).
        Dim im = SomenteNumeros(prestador.InscricaoMunicipal).PadLeft(8, "0"c)
        Dim serie = AjustarSerieId(dados.Serie)
        Dim numero = dados.Numero.ToString(CultureInfo.InvariantCulture).PadLeft(12, "0"c)
        Return "RPS" & im & serie & numero
    End Function

    Private Shared Function CriarNoCpfCnpj(doc As XmlDocument, nomeContainer As String, documento As String) As XmlElement
        ' Monta a escolha CPF/CNPJ conforme tamanho.
        Dim container = doc.CreateElement(nomeContainer, NamespaceNfe)
        Dim somente = SomenteNumeros(documento)
        Dim nomeDoc = If(somente.Length = 11, "CPF", "CNPJ")
        Dim docNode = doc.CreateElement(nomeDoc, NamespaceNfe)
        docNode.InnerText = somente
        container.AppendChild(docNode)
        Return container
    End Function

    Private Shared Sub AppendElement(pai As XmlElement, nome As String, ns As String, valor As String)
        Dim elemento = pai.OwnerDocument.CreateElement(nome, ns)
        elemento.InnerText = If(valor, String.Empty)
        pai.AppendChild(elemento)
    End Sub

    Private Shared Sub AppendElementIfNotEmpty(pai As XmlElement, nome As String, ns As String, valor As String)
        If Not String.IsNullOrWhiteSpace(valor) Then
            AppendElement(pai, nome, ns, valor)
        End If
    End Sub

    Private Shared Function FormatarValorXml(valor As Decimal) As String
        ' Formato monetario do XML com ponto decimal.
        Dim arredondado = Decimal.Round(valor, 2, MidpointRounding.AwayFromZero)
        Return arredondado.ToString("0.00", CultureInfo.InvariantCulture)
    End Function

    Private Shared Function FormatarAliquotaXml(valor As Decimal) As String
        ' Aliquota com 4 casas decimais.
        Dim arredondado = Decimal.Round(valor, 4, MidpointRounding.AwayFromZero)
        Return arredondado.ToString("0.0000", CultureInfo.InvariantCulture)
    End Function

    Private Shared Function NormalizarIssRetidoXml(valor As String) As String
        ' Booleano esperado pelo schema do RPS.
        Dim texto = If(valor, String.Empty).Trim().ToLowerInvariant()
        If texto = "s" OrElse texto = "1" OrElse texto = "true" Then
            Return "true"
        End If
        Return "false"
    End Function

    Private Shared Function AjustarSerieId(serie As String) As String
        ' Gera a serie para o Id do RPS (sem espacos).
        Dim valor = If(serie, String.Empty).Trim()
        If valor.Length = 0 Then
            Return "00000"
        End If

        Dim sb = New StringBuilder(valor.Length)
        For Each ch As Char In valor
            If Not Char.IsWhiteSpace(ch) Then
                sb.Append(ch)
            End If
        Next

        Dim resultado = sb.ToString()
        If resultado.Length > 5 Then
            resultado = resultado.Substring(0, 5)
        End If

        Return resultado.PadLeft(5, "0"c)
    End Function

    Private Shared Function SomenteNumeros(valor As String) As String
        ' Remove caracteres nao numericos para CNPJ/CPF/CEP/IM.
        If String.IsNullOrWhiteSpace(valor) Then
            Return String.Empty
        End If

        Dim sb = New StringBuilder(valor.Length)
        For Each ch As Char In valor
            If Char.IsDigit(ch) Then
                sb.Append(ch)
            End If
        Next
        Return sb.ToString()
    End Function
End Class
