Option Strict On

Imports System.Globalization
Imports System.Xml

Public NotInheritable Class RespostaEnvioRpsParser
    Private Sub New()
    End Sub

    Public Shared Function Parse(xmlResposta As String) As EnvioRpsResultado
        ' Etapa 1: preparar o resultado com o XML bruto.
        Dim resultado = New EnvioRpsResultado() With {
            .XmlResposta = xmlResposta
        }

        If String.IsNullOrWhiteSpace(xmlResposta) Then
            resultado.Sucesso = False
            resultado.Mensagem = "Resposta vazia do servico."
            Return resultado
        End If

        ' Etapa 2: carregar o XML do retorno.
        Dim doc = New XmlDocument()
        doc.LoadXml(xmlResposta)

        ' Etapa 3: configurar namespace dinamico do retorno.
        Dim nsmgr = New XmlNamespaceManager(doc.NameTable)
        If doc.DocumentElement IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(doc.DocumentElement.NamespaceURI) Then
            nsmgr.AddNamespace("nfe", doc.DocumentElement.NamespaceURI)
        End If

        ' Etapa 4: identificar sucesso ou falha do processamento.
        Dim sucessoNode = doc.SelectSingleNode("//nfe:Sucesso", nsmgr)
        If sucessoNode Is Nothing Then
            sucessoNode = doc.SelectSingleNode("//Sucesso")
        End If

        Dim sucessoTexto = If(sucessoNode?.InnerText, String.Empty).Trim().ToLowerInvariant()
        resultado.Sucesso = (sucessoTexto = "true" OrElse sucessoTexto = "1" OrElse sucessoTexto = "s")

        ' Etapa 5: coletar erros (quando existirem).
        Dim erros = doc.SelectNodes("//nfe:Erro | //Erro", nsmgr)
        If erros IsNot Nothing Then
            For Each erroNode As XmlNode In erros
                Dim codigo = ObterTexto(erroNode, "Codigo", nsmgr)
                Dim descricao = ObterTexto(erroNode, "Descricao", nsmgr)
                Dim msg = String.Empty

                If Not String.IsNullOrWhiteSpace(codigo) Then
                    msg = codigo
                End If
                If Not String.IsNullOrWhiteSpace(descricao) Then
                    If msg.Length > 0 Then
                        msg &= " - "
                    End If
                    msg &= descricao
                End If

                If msg.Length > 0 Then
                    resultado.Erros.Add(msg)
                End If
            Next
        End If

        ' Etapa 6: mensagem de retorno (quando nao houver erro).
        If resultado.Erros.Count = 0 Then
            Dim mensagemNode = doc.SelectSingleNode("//nfe:MensagemRetorno | //MensagemRetorno | //nfe:Mensagem | //Mensagem", nsmgr)
            If mensagemNode IsNot Nothing Then
                resultado.Mensagem = mensagemNode.InnerText.Trim()
            End If
        Else
            resultado.Mensagem = String.Join(" | ", resultado.Erros.ToArray())
        End If

        ' Etapa 7: extrair dados de NFS-e quando retornados.
        resultado.NumeroNFe = ObterTextoDocumento(doc, nsmgr, "//nfe:NumeroNFe | //NumeroNFe")
        resultado.CodigoVerificacao = ObterTextoDocumento(doc, nsmgr, "//nfe:CodigoVerificacao | //CodigoVerificacao")
        resultado.ChaveNFe = ObterTextoDocumento(doc, nsmgr, "//nfe:ChaveNFe | //ChaveNFe")
        resultado.ChaveRps = ObterTextoDocumento(doc, nsmgr, "//nfe:ChaveRPS | //ChaveRPS")

        Dim dataEmissaoTexto = ObterTextoDocumento(doc, nsmgr, "//nfe:DataEmissaoNFe | //DataEmissaoNFe")
        If Not String.IsNullOrWhiteSpace(dataEmissaoTexto) Then
            Dim dataEmissao As DateTime
            If DateTime.TryParse(dataEmissaoTexto, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, dataEmissao) Then
                resultado.DataEmissaoNFe = dataEmissao
            End If
        End If

        If Not resultado.Sucesso AndAlso String.IsNullOrWhiteSpace(resultado.Mensagem) Then
            resultado.Mensagem = "Falha nao identificada no retorno do servico."
        End If

        Return resultado
    End Function

    Private Shared Function ObterTextoDocumento(doc As XmlDocument, nsmgr As XmlNamespaceManager, xpath As String) As String
        Dim no = doc.SelectSingleNode(xpath, nsmgr)
        If no Is Nothing Then
            Return String.Empty
        End If
        Return If(no.InnerText, String.Empty).Trim()
    End Function

    Private Shared Function ObterTexto(noPai As XmlNode, nome As String, nsmgr As XmlNamespaceManager) As String
        If noPai Is Nothing Then
            Return String.Empty
        End If

        Dim filho = noPai.SelectSingleNode("nfe:" & nome, nsmgr)
        If filho Is Nothing Then
            filho = noPai.SelectSingleNode(nome)
        End If

        Return If(filho?.InnerText, String.Empty).Trim()
    End Function
End Class
