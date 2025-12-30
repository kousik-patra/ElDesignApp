const path = require('path')
// const HtmlWebpackPlugin = require('html-webpack-plugin')


module.exports = {
    cache: false,
    mode: 'development',
    entry: {
        main: path.resolve(__dirname, 'src/index.js'),
    },
    output: {
        path: path.resolve(__dirname, '../wwwroot/dist'),
        filename: '[name].bundle.js',
        // Add this line to output source map files
        sourceMapFilename: '[name].bundle.js.map',
        clean: true // Clean output directory before build
    },
    // Add this line to enable source map generation for development
    devtool: 'source-map',
    module: {
        rules: [
            {
                test: /\.ts$/,
                use: {
                    loader: 'ts-loader',
                    options: {
                        transpileOnly: true,
                    },
                },
                exclude: /node_modules/,
                include: [path.resolve(__dirname, 'src')]
            },
            {
                test: /\.js$/,
                use: {
                    loader: 'babel-loader',
                    options: {
                        presets: ['@babel/preset-env']
                    }
                },
                exclude: /node_modules/,
                include: [path.resolve(__dirname, 'src')]
            },
            {
                include: [path.resolve(__dirname, 'src')]
            },
        ]
    },
    resolve: {
        extensions: ['.ts', '.js'],
        fallback: {
            // Polyfills for Node.js modules if needed in CI
            "path": require.resolve("path-browserify"),
            "fs": false
        }
    }

}