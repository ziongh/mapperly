// @ts-check

const lightCodeTheme = require('prism-react-renderer/themes/github');
const darkCodeTheme = require('prism-react-renderer/themes/dracula');

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Mapperly',
  tagline: 'A .NET source generator for generating object mappings. No runtime reflection. Inspired by MapStruct.',
  url: process.env.DOCUSAURUS_URL || 'https://riok.github.io',
  baseUrl: process.env.DOCUSAURUS_BASE_URL || '/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'throw',
  favicon: 'img/logo.svg',
  organizationName: 'riok',
  projectName: 'mapperly',
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      colorMode: {
        disableSwitch: true,
      },
      navbar: {
        title: 'Mapperly',
        logo: {
          alt: 'Mapperly Logo',
          src: 'img/logo.svg',
        },
        items: [
          {
            type: 'doc',
            docId: 'intro',
            position: 'left',
            label: 'Documentation',
          },
          {
            href: 'https://github.com/riok/mapperly',
            className: 'header-github-link',
            'aria-label': 'GitHub repository',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Docs',
            items: [
              {
                label: 'Introduction',
                to: '/docs/intro',
              },
              {
                label: 'Installation',
                to: '/docs/getting-started/installation',
              },
              {
                label: 'Configuration',
                to: '/docs/category/usage-and-configuration',
              },
            ],
          },
          {
            title: 'Community',
            items: [
              {
                label: 'Open an issue',
                href: 'https://github.com/riok/mapperly/issues/new/choose'
              },
              {
                label: 'GitHub Repository',
                href: 'https://github.com/riok/mapperly',
              },
            ]
          },
          {
            title: 'More',
            items: [
              {
                label: 'NuGet',
                href: 'https://www.nuget.org/packages/Riok.Mapperly',
              },
              {
                label: 'Releases',
                href: 'https://github.com/riok/mapperly/releases',
              },
              {
                label: 'License',
                href: 'https://github.com/riok/mapperly/blob/main/LICENSE',
              },
            ],
          },
        ],
      },
      prism: {
        theme: lightCodeTheme,
        darkTheme: darkCodeTheme,
        additionalLanguages: ['csharp', 'powershell', 'editorconfig'],
      },
    }),
  plugins: [
    [
      '@docusaurus/plugin-ideal-image',
      /** @type {import('@docusaurus/plugin-ideal-image').PluginOptions} */
      ({
        max: 1600,
        min: 400,
        // Use false to debug, but it incurs huge perf costs
        disableInDev: true,
      }),
    ],
    '@easyops-cn/docusaurus-search-local',
  ],
  customFields: {
      mapperlyVersion: process.env.MAPPERLY_VERSION || '0.0.1-dev',
  }
};

module.exports = config;