Imports System.Text
Imports System.IO

''' <summary>
''' A look-up system for key/value pairs with swappable value tables, allowing for
''' easy translation and localization of strings.
''' </summary>
''' <remarks>Language files must follow the following format:
''' - Files should be in UTF-8 plain text format,
''' - One line may contain only one key/value pair,
''' - An equals-sign (=) seperates key from value, in that order,
''' - Leading and trailing whitespace is ignored/trimmed,
''' - Keys are case-insensitive,
''' - Comments are introduced by a number sign (#),
''' - Empty lines or otherwise invalid lines are ignored.</remarks>
<CLSCompliant(True)>
Public Class Localizer

    Private ReadOnly StringTable As New Dictionary(Of String, String)
    Private ReadOnly PhraseTable As New Dictionary(Of String, List(Of List(Of String)))

    Private ReadOnly RNG As New Random

    ''' <summary>
    ''' Loads a string table from a file.
    ''' </summary>
    ''' <param name="filePath">The path to the file that should be loaded.</param>
    ''' <remarks>Any previously loaded string table will be preserved.
    ''' If an exception is thrown, loading will be canceled. Entries that have already
    ''' been loaded before the exception occurred will be preserved.
    ''' Language files must follow the file format as specified in the class remarks.</remarks>
    ''' <exception cref="IOException">An exception occurred while reading the
    ''' string table file. Refer to the InnerException property for details.</exception>
    Public Sub LoadLocale(filePath As String)
        Try
            ' read all lines in the file
            Using R As New StreamReader(filePath, Encoding.UTF8)
                While Not R.EndOfStream
                    Dim Line As String = R.ReadLine
                    ' skip empty or comment lines
                    If Line.StartsWith("#", StringComparison.Ordinal) Then Continue While
                    If Line.Trim.Length < 1 Then Continue While
                    ' find the = in the line
                    Dim Parts() As String = Line.Split("="c)
                    If Parts.Count < 2 Then Continue While
                    ' remove whitespace and add key/value pair
                    StringTable.Add(Parts(0).Trim.ToUpperInvariant, Parts(1).Trim)
                End While
            End Using
        Catch ex As Exception
            Throw New IOException("Failed to read localized string table.", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Loads a phrase table from a file.
    ''' </summary>
    ''' <param name="filePath">The path to the file that should be loaded.</param>
    ''' <exception cref="IOException">An exception occurred while reading the
    ''' string table file. Refer to the InnerException property for details.</exception>
    Public Sub LoadPhrases(filePath As String)
        Try
            Using R As New StreamReader(filePath, Encoding.UTF8)
                Dim PhraseList As List(Of List(Of String)) = Nothing
                Dim WordList As List(Of String) = Nothing
                Dim PhraseName As String = Nothing
                Dim ExpectingWords As Boolean = False

                While Not R.EndOfStream
                    Dim Line As String = R.ReadLine().Trim()
                    ' skip empty or comment lines
                    If Line.StartsWith("#", StringComparison.Ordinal) Then Continue While
                    If Line.Length < 1 Then Continue While
                    ' find a command word on the line, if any
                    Dim Parts() As String = Line.Split(" "c)
                    If Parts.Count < 1 Then Continue While ' failsafe, should never happen
                    If Parts(0).Equals("phrase", StringComparison.InvariantCultureIgnoreCase) Then
                        ' start a new phrase block
                        If PhraseList IsNot Nothing Then
                            ' save any old phrase block: flush the current word list
                            If WordList IsNot Nothing Then
                                PhraseList.Add(WordList)
                                WordList = Nothing
                            End If
                            PhraseTable.Add(PhraseName, PhraseList)
                        End If
                        PhraseName = Line.Substring(7).ToUpperInvariant()
                        PhraseList = New List(Of List(Of String))
                        ExpectingWords = False

                    ElseIf Parts(0).Equals("word", StringComparison.InvariantCultureIgnoreCase) Then
                        If PhraseList Is Nothing Then Throw New InvalidDataException("Unexpected word block")
                        If WordList IsNot Nothing Then PhraseList.Add(WordList)
                        WordList = New List(Of String)
                        ExpectingWords = True

                    Else
                        If ExpectingWords Then
                            If Line.StartsWith("""") And Line.EndsWith("""") Then
                                WordList.Add(Unescape(Line))
                            Else
                                Throw New InvalidDataException(
                                        "Unexpected text: '" & Line &
                                        "'. If this line is a word in the phrase list, make sure to enclose it in quotation marks.")
                            End If
                        Else
                            Throw New InvalidDataException("Unexpected text: " & Line)
                        End If
                    End If
                End While

                ' save the last phrase block as well
                If WordList IsNot Nothing Then PhraseList.Add(WordList)
                If PhraseList IsNot Nothing Then PhraseTable.Add(PhraseName, PhraseList)
            End Using
        Catch ex As Exception
            Throw New IOException("Failed to read phrase table", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Removes all entries in the string table.
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub Clear()
        StringTable.Clear()
        PhraseTable.Clear()
    End Sub

    ''' <summary>
    ''' Looks up the specified key in the loaded string table.
    ''' </summary>
    ''' <param name="key">The key to use. Not case-sensitive.</param>
    ''' <returns>A System.String of the value of the key/value pair in the string table.
    ''' If the key does not exist or is otherwise invalid, the key is returned unchanged.</returns>
    Public Function GetString(key As String) As String
        If String.IsNullOrEmpty(key) Then Throw New ArgumentNullException("key")

        If StringTable.ContainsKey(key.ToUpperInvariant) Then
            Return StringTable(key.ToUpperInvariant)
        Else
            Return key
        End If
    End Function

    ''' <summary>
    ''' Builds a random phrase using the prototype identified with the specified key.
    ''' </summary>
    ''' <param name="key">The key of the phrase to use. Not case-sensitive.</param>
    ''' <returns>A String containing a randomly generated phrase.</returns>
    Public Function GetPhrase(key As String) As String
        If String.IsNullOrEmpty(key) Then Throw New ArgumentNullException("key")

        If PhraseTable.ContainsKey(key.ToUpperInvariant()) Then
            Dim Result As New StringBuilder
            For Each WordList As List(Of String) In PhraseTable(key.ToUpperInvariant())
                Result.Append(WordList(RNG.Next(WordList.Count)))
            Next
            Return Result.ToString()
        Else
            Return key
        End If
    End Function

    ''' <summary>
    ''' Removes leading and trailing quotation marks from the specified string, while
    ''' also unescaping a few common character escape sequences.
    ''' </summary>
    ''' <param name="text">The string to unquote.</param>
    Private Function Unescape(text As String) As String
        If String.IsNullOrEmpty(text) Then Throw New ArgumentNullException("text")
        Return text.Trim(""""c).Replace("\""", """").Replace("\n", Environment.NewLine)
    End Function

End Class
