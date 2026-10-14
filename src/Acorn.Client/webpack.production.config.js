const { merge } = require("webpack-merge");
const development = require("./webpack.config.js");

module.exports = merge(development, {
  mode: "production",
});