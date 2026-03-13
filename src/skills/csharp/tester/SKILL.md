---
name: tester
description: Schreibt, führt aus und vervollständigt Unit-Tests für C#/.NET Code. Nutzt einen zweiten Agent um fehlende Test-Cases zu identifizieren. Verwende diesen Skill, wenn du aufgefordert wirst, Tests zu erstellen oder die Testabdeckung zu verbessern.
---

Schreibe umfassende Unit-Tests für den angegebenen Code. Folge dabei einem mehrstufigen Prozess mit automatischer Identifikation fehlender Test-Cases.

## Konventionen

- **Test-Framework**: xUnit
- **Mocking**: FakeItEasy
- **Assertions**: AwesomeAssertions
- **Struktur**: Jede Testmethode hat Arrange/Act/Assert-Blöcke, markiert mit Kommentaren
- **Sprache**: Englisch für Code, Kommentare und Testnamen
- **Stil**: Übernimm den bestehenden Test-Stil aus nahegelegenen Test-Dateien im Projekt

## Phase 1: Tests schreiben

1. **Code analysieren**: Lies den zu testenden Code und verstehe:
  - Öffentliche API (Methoden, Properties)
  - Abhängigkeiten (was muss gemockt werden?)
  - Verschiedene Code-Pfade (if/else, switch, exceptions)
  - Edge Cases (null, leere Collections, Grenzwerte)

2. **Testprojekt identifizieren**: Finde das passende Testprojekt unter `source/Test/`. Orientiere dich an der bestehenden Projektstruktur.

3. **Tests erstellen**: Schreibe Tests nach diesem Muster:

```csharp
public class MeineKlasseTests
{
    [Fact]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange
        var dependency = A.Fake<IDependency>();
        A.CallTo(() => dependency.DoSomething()).Returns(expectedValue);
        var sut = new MeineKlasse(dependency);

        // Act
        var result = sut.MethodUnderTest(input);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("input1", "expected1")]
    [InlineData("input2", "expected2")]
    public void MethodName_WithVariousInputs_ReturnsExpected(string input, string expected)
    {
        // Arrange
        var sut = new MeineKlasse();

        // Act
        var result = sut.MethodUnderTest(input);

        // Assert
        result.Should().Be(expected);
    }
}
```

4. **Test-Kategorien abdecken**:
  - Happy Path (normaler Erfolgsfall)
  - Fehlerbehandlung (Exceptions, ungültige Eingaben)
  - Null-/Leer-Eingaben
  - Grenzwerte (Boundary Conditions)
  - Abhängigkeits-Verhalten (Mocks, verschiedene Rückgabewerte)

## Phase 2: Tests ausführen

1. Führe `dotnet test` im relevanten Testprojekt aus
2. Analysiere die Ergebnisse:
  - Bei **Fehlern**: Identifiziere die Ursache und fixe den Test oder den Testaufbau
  - Bei **Erfolg**: Weiter zu Phase 3
3. Wiederhole bis alle Tests grün sind

## Phase 3: Fehlende Test-Cases identifizieren

Starte einen **separaten Agent**, der die geschriebenen Tests analysiert und fehlende Cases identifiziert.

Der Agent soll:

1. Den zu testenden Produktionscode lesen
2. Die geschriebenen Tests lesen
3. Eine priorisierte Liste fehlender Test-Cases zurückgeben, kategorisiert nach:

  - **Fehlende Edge Cases**: Null-Werte, leere Strings, leere Collections, Maximalwerte
  - **Fehlende Fehlerpfade**: Exception-Szenarien, Timeout-Verhalten, Fehler in Abhängigkeiten
  - **Fehlende Boundary Conditions**: Grenzwerte, Off-by-one, Integer-Overflow
  - **Fehlende Interaktionen**: Reihenfolge von Aufrufen, Mehrfachaufrufe, Concurrent Access
  - **Fehlende Zustandsübergänge**: Verschiedene Ausgangszustände, State-Maschinen

Format der Rückgabe:
```
1. [HOCH] MethodName - Szenario: Beschreibung des fehlenden Tests
2. [MITTEL] MethodName - Szenario: Beschreibung des fehlenden Tests
3. [NIEDRIG] MethodName - Szenario: Beschreibung des fehlenden Tests
```

## Phase 4: Fehlende Tests ergänzen

1. Implementiere die identifizierten fehlenden Tests (priorisiert: HOCH → MITTEL → NIEDRIG)
2. Führe erneut `dotnet test` aus
3. Fixe eventuelle Fehler
4. Stelle sicher, dass alle Tests grün sind

## Ausgabeformat

Am Ende gib eine Zusammenfassung aus:

```
### Test-Ergebnis

**Phase 1**: X Tests geschrieben
**Phase 2**: Alle Tests grün ✅
**Phase 3**: Y fehlende Cases identifiziert (Z Hoch, W Mittel, V Niedrig)
**Phase 4**: Y zusätzliche Tests implementiert, alle grün ✅

**Gesamt**: X + Y Tests, alle bestanden
```

## Wichtige Hinweise

- Schreibe **keine Tests für triviale Getter/Setter** ohne Logik
- Mocke **keine Werttypen** oder einfache DTOs – erstelle echte Instanzen
- Teste **Verhalten**, nicht Implementierungsdetails
- Verwende **sprechende Testnamen** im Format `MethodName_Scenario_ExpectedBehavior`
- Bei `[Theory]`-Tests: Verwende `[InlineData]` für einfache Typen, `[MemberData]` für komplexe Objekte
- Nutze `A.CallTo(...).MustHaveHappened()` sparsam – nur wenn der Aufruf das erwartete Verhalten ist
