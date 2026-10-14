const path = require('path');
module.exports = {
  mode: 'development',
  entry: {
    'all': './src/pages/all.ts'
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: [
          'babel-loader',
          {
            loader: 'ts-loader',
            options: {
              transpileOnly: false, // Enable type checking
            }
          }
        ],
        exclude: /node_modules/,
      },
      {
        test: /\.css$/i,
        use: ["style-loader", "css-loader"],
      },
    ],
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js', '.jsx'],
  },
  output: {
    filename: '[name].js',
    path: path.resolve(__dirname, '../Acorn/wwwroot/js/pages/'),
  },
  devtool: 'source-map'
};
