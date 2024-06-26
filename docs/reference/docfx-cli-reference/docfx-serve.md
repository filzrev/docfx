# docfx serve

## Name

`docfx serve [directory] [OPTIONS]` - Host a local static website.

## Usage

```pwsh
docfx serve [directory] [OPTIONS]
```

Run `docfx serve --help` or `docfx -h` to get a list of all available options.

## Arguments

- `[directory]` <span class="badge text-bg-primary">optional</span>

   Path to the directory to serve.
   If not specified. Current directory is used.

## Options

- **-h|--help**

    Prints help information

- **-n|--hostname.**

  Specify the hostname of the hosted website

- **-p|--port.**

  Specify the port of the hosted website

- **--open-browser.**

  Open a web browser when the hosted website starts.

- **--open-file<RELATIVE_PATH>.**

  Open a file in a web browser when the hosted website starts.

## Examples

- Host a website that generated by `docfx build` command.

```pwsh
docfx serve _site
```

- Host a website that generated by `docfx build` command. And launch default browser.

```pwsh
docfx --serve _site --open-browser
```
