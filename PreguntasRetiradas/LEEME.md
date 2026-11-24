# Preguntas retiradas

Preguntas de selección múltiple que se sacaron del banco local (`Assets/Resources/Questions`) en el tercer documento de ajustes, porque su enunciado no cabe en 2 filas de 73 caracteres: el código de la pregunta ocupa varias líneas.

- Los archivos tienen el mismo formato que el banco (`{"questions": [...]}`) y el mismo nombre que el banco del que salieron.
- Conservan su `id`, sus respuestas y su versión en inglés.
- Están fuera de `Assets`, así que Unity no las importa y no entran al juego.

Se pueden volver a usar si la vista de selección múltiple tiene un recuadro para el código, como la de completar el código. Otra opción es reescribir el enunciado para que quepa.
