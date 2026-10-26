const path = require("path");
// A separately installed build toolchain may be reused read-only during development.
const modules = process.env.MW_UI_TOOLCHAIN || path.join(__dirname, "node_modules");
const webpack = require(path.join(modules, "webpack"));
const MiniCssExtractPlugin = require(path.join(modules, "mini-css-extract-plugin"));
module.exports = {
    mode: "production", entry: "./src/index.tsx",
    externalsType: "window",
    externals: { react: "React", "react-dom": "ReactDOM", "cs2/modding": "cs2/modding", "cs2/api": "cs2/api", "cs2/ui": "cs2/ui", "cs2/input":"cs2/input" },
    module: { rules: [
        { test: /\.png$/, type: "asset/inline" },
        { test: /\.tsx?$/, use: { loader: "ts-loader", options: { configFile: "tsconfig.build.json" } }, exclude: /node_modules/ },
        { test: /\.scss$/, use: [MiniCssExtractPlugin.loader, "css-loader", "sass-loader"] }
    ] },
    resolve: { extensions: [".tsx", ".ts", ".js"], modules: [modules, "node_modules"] },
    resolveLoader: { modules: [modules, "node_modules"] },
    output: { path: path.resolve(__dirname, "../dist"), filename: "MW.EditorTools.mjs", library: { type: "module" }, publicPath: "coui://ui-mods/" },
    experiments: { outputModule: true },
    plugins: [new MiniCssExtractPlugin({ filename: "MW.EditorTools.css" }), new webpack.BannerPlugin({
        banner: "/*\n * Cities: Skylines II UI Module\n *\n * Id: MW.EditorTools\n * Author: MW\n * Version: 0.4.251\n * Dependencies:\n */", raw: true,
        stage: webpack.Compilation.PROCESS_ASSETS_STAGE_REPORT
    })]
};






