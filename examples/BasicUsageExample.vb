[ActionLinks]
ACTIONID=0651ac2a-5bef-456b-b94a-a824711030ba
DATABASE=QW_ICIM
ICALENDAR=0
JSON=1
NOME=ReadWrite_Given_destinationId
SCRIPT=dim CheckToken = false
Dim StringPayload = "" : Dim ParsedPayload
dim PayloadAndTokenValidationError = DB.Global.ValidatePayloadAndToken(DB,CheckToken,"",ParsedPayload,StringPayload)
if PayloadAndTokenValidationError IsNot Nothing Then
    DB.Global.LogCustom(DB,StringPayload,PayloadAndTokenValidationError,"Error at ValidatePayloadAndToken: ")
    return PayloadAndTokenValidationError
end if
Dim DestinationIdentifierInfo = DB.Global.GetDestinationIdentifier(ParsedPayload)
'-----------------------------------------------------------------------------------------------------------------------
'Handle destinationId values or it's absence
'''''''

''''''
If DestinationIdentifierInfo.Item1 Then
    Dim destinationId As String = DestinationIdentifierInfo.Item2

    ' No type references needed at all!
    ' Use Object instead of ParameterCondition type
    Dim searchConditions As New System.Collections.Generic.Dictionary(Of String, Object)
    
    searchConditions.Add("tipo", DB.Global.CreateParameterCondition(
        "tipo",
        "tipo like :tipo",
        Nothing
    ))
    
    searchConditions.Add("numero", DB.Global.CreateParameterCondition(
        "numero",
        "numero = :numero",
        Nothing
    ))
    searchConditions.Add("minDtPubbl", DB.Global.CreateParameterCondition(
        "minDtPubbl",
        "DATA_PUBBL >= :minDtPubbl",
        Nothing
    ))
        searchConditions.Add("maxDtPubbl", DB.Global.CreateParameterCondition(
        "maxDtPubbl",
        "DATA_PUBBL <= :maxDtPubbl",
        Nothing
    ))

    If destinationId = "document-read" Then
        Return DB.Global.ProcessActionLink(DB,
            DB.Global.CreateValidator(New String() {"maxDtPubbl"}),
            DB.Global.CreateBusinessLogicForReading(
                "SELECT tipo, numero, DATA_PUBBL FROM document {WHERE} ORDER BY data_pubbl ASC",
                searchConditions,
                "1=0"
            ),
            "Search executed.",
            ParsedPayload, StringPayload, False)

    Else
        Return DB.Global.CreateErrorResponse("'" & destinationId & "' is not a valid DestinationIdentifier...")
    End If
Else
    Dim errorMessage As String = DestinationIdentifierInfo.Item2
    Return DB.Global.CreateErrorResponse(errorMessage)
End If

' DEMO PAYLOADS
' WRITE
' { "DestinationIdentifier":"P_MatricoleCAT-WRITE",
'     "Records":[
'     {
'     "CODICE_CAT":"test1231",
'     "MATRICOLA":"Mat1234",
'     "Prodotto":"prod1",
'     "DataUtilizzo":"2025/12/10w"},
'        {
'     "CODICE_CAT":"test12312",
'     "MATRICOLA":"Mat12344",
'     "Prodotto":"prod1",
'     "DataUtilizzo":"2025/12/10"}
' ]}
' READ
' {   "DestinationIdentifier":"P_MatricoleCAT-READ",
'     "MATRICOLA": "MAT%",
'     "CODICE_CAT": "%"
' }
