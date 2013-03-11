Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Namespace Armory

    Public Class Career

        Public Event SendUpdate(ByVal text As String)

        Public ReadOnly Property Count As Integer
            Get
                Return _heroes.Count
            End Get
        End Property

        Public ReadOnly Property Heroes As List(Of IHero)
            Get
                Return _heroes
            End Get
        End Property

        Private ReadOnly _heroes As New List(Of IHero)

        Public Sub Initialize(ByVal battleTag As String)
            Dim jo As JObject
            Dim url As String
            Dim host As String = "http://us.battle.net/api/d3/profile/" & battleTag & "/"
            jo = GetJObject(host)
            For Each obj In jo("heroes")
                url = host & "hero/" & obj("id").Value(Of Integer)()
                _heroes.Add(New Hero(url))
                RaiseEvent SendUpdate("Loaded " & _heroes.Last.Name & " <" & _heroes.Last.ParagonLevel & ">")
            Next
        End Sub

        Private Class Hero

            Implements IHero

            Public ReadOnly Property ActiveSkills As List(Of IActive) Implements IHero.ActiveSkills
                Get
                    Return _activeSkills
                End Get
            End Property
            Public ReadOnly Property PassiveSkills As List(Of IPassive) Implements IHero.PassiveSkills
                Get
                    Return _passiveSkills
                End Get
            End Property
            Public ReadOnly Property Stats As List(Of IProp(Of Object)) Implements IHero.Stats
                Get
                    Return _props
                End Get
            End Property
            Public ReadOnly Property Items As List(Of IItem) Implements IHero.Items
                Get
                    Return _items
                End Get
            End Property
            Public ReadOnly Property Id As Double Implements IHero.Id
                Get
                    Return GetVal(Of Double)("id")
                End Get
            End Property
            Public ReadOnly Property Name As String Implements IHero.Name
                Get
                    Return GetVal(Of String)("name")
                End Get
            End Property
            Public ReadOnly Property [Class] As String Implements IHero.Class
                Get
                    Return GetVal(Of String)("class")
                End Get
            End Property
            Public ReadOnly Property Gender As Double Implements IHero.Gender
                Get
                    Return GetVal(Of Double)("gender")
                End Get
            End Property
            Public ReadOnly Property Level As Double Implements IHero.Level
                Get
                    Return GetVal(Of Double)("level")
                End Get
            End Property
            Public ReadOnly Property ParagonLevel As Double Implements IHero.ParagonLevel
                Get
                    Return GetVal(Of Double)("paragonLevel")
                End Get
            End Property
            Public ReadOnly Property Hardcore As Boolean Implements IHero.Hardcore
                Get
                    Return GetVal(Of Boolean)("hardcore")
                End Get
            End Property
            Public ReadOnly Property Dead As Boolean Implements IHero.Dead
                Get
                    Return GetVal(Of Boolean)("dead")
                End Get
            End Property
            Public ReadOnly Property Lastupdated As Double Implements IHero.LastUpdated
                Get
                    Return GetVal(Of Double)("last-updated")
                End Get
            End Property

            Private ReadOnly _profile As JObject
            Private ReadOnly _activeSkills As New List(Of IActive)
            Private ReadOnly _passiveSkills As New List(Of IPassive)
            Private ReadOnly _props As New List(Of IProp(Of Object))
            Private ReadOnly _items As New List(Of IItem)

            Public Sub New()

            End Sub

            Public Sub New(ByVal url As String)
                Dim jo As JObject = Nothing
                jo = GetJObject(url)
                _profile = jo
                For Each obj In jo
                    _props.Add(New Prop(Of Object)(jo, obj.Key))
                Next
                jo = _profile("stats")
                For Each obj In jo
                    _props.Add(New Prop(Of Object)(jo, obj.Key))
                Next
                jo = _profile("items")
                If jo.HasValues Then
                    For Each obj In jo
                        _items.Add(New Item(jo, obj.Key))
                    Next
                End If
                'ImportSkills()
                'CalculateAllResist()
            End Sub

            Private Function GetVal(Of T)(ByVal name As String) As T
                For Each p In _props
                    If p.Name = name Then
                        Return p.Value.value
                    End If
                Next
            End Function

            Private Class Prop(Of T) : Implements IProp(Of T)

                Public Property Value As T Implements IProp(Of T).Value
                    Get
                        Return _value
                    End Get
                    Set(ByVal val As T)
                        _value = val
                    End Set
                End Property

                Public ReadOnly Property Name As String Implements IProp(Of T).Name
                    Get
                        Return _name
                    End Get
                End Property
                Public ReadOnly Property RangeName As String Implements IProp(Of T).RangeName
                    Get
                        Return _rangeName
                    End Get
                End Property

                Private ReadOnly _name As String
                Private ReadOnly _rangeName As String
                Private _value As T

                ''' <summary>
                ''' Initializes a new instance of the class.
                ''' </summary>
                ''' <param name="obj">The obj.</param>
                ''' <param name="name">The name.</param>
                Public Sub New(ByVal obj As JObject, ByVal name As String)
                    _name = name
                    _rangeName = "Hero_" & name
                    _value = obj(name).Value(Of T)()
                End Sub

            End Class

            Private Class Item
                Implements IItem
                Public ReadOnly Property Attributes As Dictionary(Of String, IAttribute) Implements IItem.Attributes
                    Get
                        Return _attr
                    End Get
                End Property
                Public ReadOnly Property Data As Dictionary(Of String, String) Implements IItem.Data
                    Get
                        Return _data
                    End Get
                End Property
                Public ReadOnly Property Slot As String Implements IItem.Slot
                    Get
                        Return _slot
                    End Get
                End Property
                Private ReadOnly _slot As String
                Private ReadOnly _data As Dictionary(Of String, String) = New Dictionary(Of String, String)
                Private ReadOnly _attr As Dictionary(Of String, IAttribute) = New Dictionary(Of String, IAttribute)
                Public Sub New(ByVal itemsJObj As JObject, ByVal slot As String)
                    Dim dataJObj As JObject = itemsJObj(slot)
                    Dim attrJObj As JObject
                    _slot = slot
                    _data.Add("name", dataJObj("name").Value(Of String)())
                    _data.Add("icon", dataJObj("icon").Value(Of String)())
                    _data.Add("displayColor", dataJObj("displayColor").Value(Of String)())
                    _data.Add("tooltipParams", dataJObj("tooltipParams").Value(Of String)())
                    attrJObj = GetJObject("http://us.battle.net/api/d3/data/" & _data("tooltipParams"))
                    If attrJObj IsNot Nothing Then
                        attrJObj = attrJObj("attributesRaw")
                        For Each obj In attrJObj
                            _attr.Add(obj.Key, New Attribute(_slot, obj))
                        Next
                    End If
                End Sub
                Private Class Attribute
                    Implements IAttribute
                    Public Property Value As Double Implements IAttribute.Value
                        Get
                            Return _value
                        End Get
                        Set(ByVal value As Double)
                            _value = value
                        End Set
                    End Property
                    Public Property Name As String Implements IAttribute.Name
                        Get
                            Return _name
                        End Get
                        Set(ByVal v As String)
                            _name = v
                        End Set
                    End Property
                    Public Property RangeName As String Implements IAttribute.RangeName
                        Get
                            Return _rangeName
                        End Get
                        Set(ByVal v As String)
                            _rangeName = v
                        End Set
                    End Property
                    Public Property Shown As Boolean Implements IAttribute.Shown
                        Get
                            Return _shown
                        End Get
                        Set(ByVal v As Boolean)
                            _shown = v
                        End Set
                    End Property
                    Public Property Slot As String Implements IAttribute.Slot
                        Get
                            Return _slot
                        End Get
                        Set(ByVal v As String)
                            _slot = v
                        End Set
                    End Property
                    Private _slot As String
                    Private _value As Double
                    Private _name As String
                    Private _rangeName As String
                    Private _shown As Boolean = False
                    Public Sub New(ByVal slot As String, ByVal attr As KeyValuePair(Of String, JToken))
                        _slot = slot
                        _name = attr.Key
                        _value = attr.Value("max").Value(Of Double)()
                        _rangeName = _slot & "_" & Regex.Replace(_name, "#|!|@|&", "_")
                    End Sub
                End Class
            End Class

        End Class

        Private Shared Function GetJObject(ByVal url As String) As JObject
            Dim jo As JObject = Nothing
            Dim uri As Uri
            Dim json As String = ""
            Dim client = New WebClient
            client.Proxy = Nothing
            uri = New Uri(url)
            Do
                Try
                    json = client.DownloadString(uri)
                    jo = JObject.Parse(json)
                Catch ex As Exception
                    jo = Nothing
                End Try
            Loop Until jo IsNot Nothing
            Return jo
        End Function

    End Class

End Namespace
