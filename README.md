# PeliCode

Juego en Unity para practicar programación en C#. El jugador inicia sesión, elige una categoría y una dificultad, y responde preguntas de cuatro tipos con tiempo. En el modo ranked las preguntas se mezclan y la partida sigue hasta el primer error. El mejor puntaje entra al Top 25.

El juego está en español y en inglés.

## Requisitos

- Unity **6000.3.19f1** (URP 2D).
- Un proyecto de Unity Gaming Services vinculado (Project Settings > Services) con:
  - **Authentication**, con el proveedor *Username & Password* activado.
  - **Cloud Save**, para el perfil del jugador, las preguntas y los anuncios.
  - **Leaderboards**, con un leaderboard de ID `ranked_top_score` (orden descendente, *Keep best*).

## Escenas

En `Assets/Scenes`, en este orden en Build Settings:

1. **Login**: inicio de sesión y registro. Contiene el GameObject `Managers` con los managers que persisten entre escenas.
2. **Menu**: cinco paneles que se deslizan (Anuncios, Perfil, Minijuegos, Ranked y Opciones).
3. **MiniGame**: la partida.

Todos los cambios de escena pasan por `SceneLoader`, que usa `SceneTransition`:

1. La escena actual se funde a negro.
2. Aparece la pantalla de carga: "CARGANDO" con los puntos de 1 a 3 y un cuadrado blanco que gira, como placeholder para un logo.
3. La pantalla de carga se mantiene hasta que la escena nueva está lista. Una escena que necesita preparar algo implementa `ISceneReadiness`; el minijuego, por ejemplo, espera a tener las preguntas.
4. Se funde a negro y aparece la escena nueva.

La pantalla de carga se construye por código, así que no hay que agregar nada a las escenas.

## Estructura del código (`Assets/Scripts`)

| Carpeta | Qué hay |
| --- | --- |
| `Authentication` | Login y registro con Unity Authentication. El correo se convierte en un hash que sirve de nombre de usuario, y la validación de contraseña es la misma que exige Unity. |
| `CloudServices` | Cloud Save (perfil y preguntas) y Leaderboards. |
| `Core` | `GameManager`, inicio de Unity Services, cambio de escenas y el `Singleton` base. |
| `Data` | Modelos (`PlayerProfileData`, `QuestionData`) y reglas puras: rachas, reseteo semanal y validación de preguntas. |
| `Localization` | Sistema de idiomas: `Loc`, `LocalizedText` y el botón de idioma. |
| `Menu` | Paneles del menú. |
| `MiniGame` | `MiniGameManager`, cronómetro, puntaje y las cuatro vistas de pregunta. |
| `UI` | Paleta (`UITheme`), formato de código y navegación con teclado en formularios. |
| `Audio` | Efectos de sonido (generados por código) y volúmenes. |

## Idiomas

Los textos de la interfaz están en `Assets/Resources/Localization/Textos.txt`, con una fila por texto: `clave`, `es` y `en`, separados por **tabuladores**. Se puede editar en una hoja de cálculo.

- Un texto fijo de una escena lleva el componente `LocalizedText` con su clave.
- Un texto armado desde código usa `Loc.Get("clave")` o `Loc.Format("clave", valores)`.
- El idioma se cambia con el botón de Login o en Opciones, y queda guardado. La primera vez se usa el del sistema.

Las pruebas avisan si falta una traducción o si una escena o el código usan una clave que no está en la tabla.

## Preguntas

Cada categoría y dificultad es una entrada de **Cloud Save > Custom Items**:

- **Custom ID** y **clave**: `questions_{Categoria}_{Dificultad}`, por ejemplo `questions_Condicionales_Facil`.
- **Valor**: un JSON `{"questions": [...]}`.

Categorías: `VariablesTiposOperadores`, `Condicionales`, `SwitchEstados`, `ColeccionesArraysListas` y `BucleWhileDoWhile`. Dificultades: `Facil`, `Medio` y `Dificil`.

Campos de cada pregunta (`QuestionData`):

| Tipo (`type`) | Campos |
| --- | --- |
| `SeleccionMultiple` | `correctAnswer` y `incorrectAnswerPool` (lo ideal son 8 incorrectas; se muestran 3 al azar). |
| `CompletarCodigo` | `codeTemplate`, con `____` donde va la respuesta, y `expectedAnswer`. |
| `ArrastrarCodigo` | `codeBlocks` y `correctOrder` (índices de `codeBlocks` en orden). **Máximo 8 bloques**, porque caben 2 filas de 4; una pregunta con más se descarta. No conviene repetir líneas idénticas, porque el orden queda ambiguo. |
| `DetectarError` | `codeSnippetsOptions` y `buggyIndex`. |

Todas llevan `id`, `type` y `questionText`. Una pregunta mal cargada se descarta con un aviso en la consola, en vez de colgar la partida.

### Preguntas en inglés

El texto de cada pregunta viene del Dashboard, así que la traducción también va ahí, en la misma pregunta. Si a una pregunta le falta la versión en inglés, el juego en inglés la muestra en español.

- `questionTextEn`: el enunciado en inglés. Sirve para todos los tipos.
- `correctAnswerEn` e `incorrectAnswerPoolEn`: para selección múltiple, cuando las respuestas son texto y no código. Van juntos; si falta uno, se usan los dos en español.
- El código (`codeTemplate`, `expectedAnswer`, `codeBlocks` y `codeSnippetsOptions`) no se traduce: es el mismo en los dos idiomas.

Ejemplo:

```json
{
  "id": "loop-medio-7",
  "type": "SeleccionMultiple",
  "questionText": "¿Qué hace continue dentro de un while?",
  "questionTextEn": "What does continue do inside a while loop?",
  "correctAnswer": "Salta a la siguiente vuelta",
  "incorrectAnswerPool": ["Termina el bucle", "Repite la vuelta actual", "Termina el programa"],
  "correctAnswerEn": "Skips to the next iteration",
  "incorrectAnswerPoolEn": ["Ends the loop", "Repeats the current iteration", "Ends the program"]
}
```

Los 150 archivos de `Assets/Resources/Questions` ya traen los dos idiomas y sirven de modelo.

### Banco local

`Assets/Resources/Questions/` trae un banco de ejemplo: 150 preguntas, las que pide cada partida (5 en Fácil, 10 en Normal y 15 en Difícil por categoría), en los dos idiomas. Las respuestas se comprobaron con el compilador de C#: las salidas, los fragmentos que no compilan y los que fallan al ejecutarse. Los archivos tienen el mismo formato que el valor en Cloud Save, así que sirven para copiarlos al Dashboard. El juego los usa cuando Cloud Save falla o no tiene preguntas para esa categoría y dificultad. Si Cloud Save tiene preguntas, esas tienen prioridad.

Para probar el minijuego sin cuenta ni conexión, abre la escena **MiniGame** en el editor y dale Play: usa el banco local y no guarda nada.

## Anuncios

Es una entrada de Cloud Save > Custom Items con Custom ID y clave `announcements`. El valor puede ser un texto, que se muestra igual en los dos idiomas, o un objeto `{"es": "...", "en": "..."}`.

## Pruebas

Hay pruebas EditMode en `Assets/Tests/Editor`: validación, rachas, opciones de selección múltiple, arrastrar bloques, idiomas, banco de preguntas y una partida corta con el banco local.

- Desde Unity: Window > General > Test Runner > EditMode > Run All.
- Por consola:

```
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults resultados.xml
```

## Créditos

DOTween (Demigiant) para las animaciones del menú.
