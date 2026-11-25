# JavaViva — Tus juegos Java de los 2000, en tu PC

JavaViva es un programa para Windows que vuelve a abrir tus juegos Java (`.jar`) de los móviles de los años 2000, como si los jugaras en tu Nokia, Sony Ericsson o Motorola de antes.

**Descargar la última versión (v1.1.7):** https://github.com/AngelRM1/JavaViva-Windows/releases/latest

También hay una versión que corre en el navegador, sin instalar nada: https://interactivo.nouslogic.xyz/javaviva

## Descargar e instalar

1. Descarga el archivo ZIP (98 MB) desde el enlace de arriba.
2. Descomprímelo en una carpeta de tu PC (clic derecho → Extraer todo). No es un instalador: no escribe nada en el sistema.
3. Abre `JavaViva.exe` y listo. No necesitas instalar Java: el programa ya lleva el suyo incluido.
4. Windows puede avisar con SmartScreen porque el programa no tiene firma de pago. Es normal en un programa nuevo. Si confías en el origen, pulsa "Más información" → "Ejecutar de todos modos".

Requisitos: Windows 10 u 11 de 64 bits. El `.exe` solo no basta: necesita las carpetas `engine/` y `runtime/` que vienen en el ZIP, junto a él.

## Cómo jugar (5 minutos)

1. En la pestaña **Biblioteca**, pulsa **Agregar .jar** y elige tu juego. También puedes copiar tus archivos `.jar` a la carpeta `Juegos/` y pulsar **Refrescar**.
2. Elige el juego en la lista. La resolución se detecta sola; si se ve cortado, chico o en blanco, cámbiala en **Configuración** o con los botones de prueba rápida.
3. Pulsa **▶ Jugar**. El juego se abre en su propia ventana.
4. Si el juego pregunta "Enable Sound?", pulsa **Q** (la tecla suave izquierda).
5. Usa las flechas para moverte y **Enter** para aceptar. **Esc** abre el menú del juego.

La biblioteca empieza vacía: JavaViva no incluye juegos. Agrega solo archivos `.jar` que tengas legalmente (juegos que compraste, copias de respaldo de juegos que compraste, o juegos libres).

## Teclas

- **Q / W:** teclas suaves izquierda / derecha.
- **Flechas:** moverte. **Enter o 5:** aceptar / disparar.
- **1–9, 0:** teclas del teléfono. **E:** `*`, **R:** `#`, **A:** borrar.
- **Esc:** menú del juego. **Espacio:** avance rápido. **X:** pausa. **+ / −:** zoom.
- En la pestaña **Teclas** hay un teclado del teléfono en pantalla: pulsa un botón y el juego recibe esa tecla. También puedes cambiar qué tecla física hace cada cosa.

## Tus juegos, favoritos y respaldos

- **Favoritos:** marca tus juegos favoritos con la estrella; van primeros en la lista y puedes filtrar solo esos.
- **Buscar:** escribe en la caja de búsqueda para encontrar un juego por nombre, fabricante o archivo.
- **Captura de pantalla:** con un juego corriendo, pulsa el botón de captura y se guarda una foto de la ventana del juego en la carpeta `Capturas`.
- **Respaldo:** en Biblioteca puedes exportar tu lista de juegos con sus ajustes a un archivo, e importarla después sin perder nada.
- Tus juegos y ajustes se guardan en tu PC (en `%AppData%\JavaViva`). Nada se sube a ningún servidor.

## Ajustes por juego

Cada juego guarda sus propios ajustes: resolución (automática, de la lista o a tu medida), tamaño de ventana (1x, 2x, 3x), giro de pantalla, sonido, límite de imágenes por segundo, luz de fondo y perfil de teléfono (cámbialo solo si el juego fue hecho para esa marca). También hay opciones de compatibilidad para los juegos que fallan.

## Si algo no funciona

- **El juego se ve cortado, chico o en blanco:** ciérralo y prueba otra resolución en Configuración (o los botones de prueba rápida en Biblioteca).
- **El juego se queda en blanco:** dentro del juego, pulsa Esc → Settings → Compatibility Settings y activa "Repaint on MIDP Display setCurrent".
- **No arranca ningún juego:** revisa que las carpetas `engine/` y `runtime/` estén junto al `.exe`. Si falta Java, JavaViva lo avisa en el encabezado.
- **No todos los juegos funcionan:** cada juego se hizo para un teléfono concreto. Prueba otra resolución o perfil de teléfono.

## Actualizaciones

Al abrir JavaViva, el programa revisa si hay una versión nueva. Si la hay, el botón "Buscar actualizaciones" del encabezado se pone verde: púlsalo y te lleva a la descarga. También puedes comprobarlo a mano con ese botón.

## Donaciones (voluntarias)

JavaViva es gratis y lo seguirá siendo. Si quieres apoyar el proyecto: PayPal (paypal.me/engelarm), Buy Me a Coffee (buymeacoffee.com/engelarm) o nouslogic.xyz/dona-un-cafe. Las donaciones no desbloquean nada: son solo apoyo.

## Límites honestos

- JavaViva es solo el programa (emulador): no incluye, descarga ni vende juegos.
- No convierte `.jar` a `.apk` ni a `.exe`.
- Un archivo `.jar` puede ejecutar código en tu PC como cualquier programa: usa solo archivos de fuentes en las que confíes.
- Los juegos 3D van mejor en Windows que en el navegador, pero no todos los juegos corren: cada uno se hizo para un teléfono concreto.

## Créditos

- Motor: FreeJ2ME-Plus v1.53 (GPLv3, código abierto) — https://github.com/hex007/freej2me
- Java incluido: Temurin 21 (Adoptium).
- Creado por NousLogic — https://nouslogic.xyz

## Novedades

- **v1.1.7:** sin sección de terminal; todos los botones del mismo tamaño; el aviso de nueva versión ahora está en el encabezado.
- **v1.1.6:** el juego ya no aparece como "cerrado" mientras juegas; no se pueden correr dos juegos a la vez; la captura de pantalla funciona mientras juegas; las pestañas ya no se ven blancas.
- **v1.1.5:** favoritos con estrella, búsqueda, captura de pantalla y respaldo de la biblioteca.
- **v1.1.4:** aviso de nueva versión al abrir el programa.

## Para desarrolladores

El código fuente está en `src/` (C#, .NET 8, WinForms). Cómo compilarlo, el motor y las notas técnicas están en [docs/NOTAS-TECNICAS.md](docs/NOTAS-TECNICAS.md).
