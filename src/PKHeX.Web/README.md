# PKHeX.Web

A proof-of-concept with Blazor WASM

## Development

From the repository root, install submodules, .NET dependencies, and both JavaScript asset projects:

```sh
./setup-dev.sh
```

Start the Blazor application:

```sh
dotnet watch run --project src/PKHeX.Web --no-hot-reload
```

To rebuild the `_js` assets continuously while changing JavaScript, run this in another terminal:

```sh
npm run build:watch --prefix src/PKHeX.Web/_js
```
