import { extendTheme, ThemeConfig } from '@chakra-ui/react'

const config: ThemeConfig = {
  initialColorMode: 'dark',
  useSystemColorMode: false,
}

export const theme = extendTheme({
  config,
  fonts: {
    heading: 'Inter, system-ui, sans-serif',
    body: 'Inter, system-ui, sans-serif',
  },
  styles: {
    global: {
      body: {
        bg: 'gray.900',
        color: 'gray.100',
      },
      '*': {
        transition: 'all 0.15s ease-out',
      },
    },
  },
  components: {
    Button: {
      baseStyle: {
        rounded: 'md',
        fontWeight: 'medium',
        transition: 'all 0.15s ease-out',
        _hover: { transform: 'translateY(-1px)' },
      },
      defaultProps: {
        colorScheme: 'teal',
      },
    },
    Card: {
      baseStyle: {
        container: {
          bg: 'gray.800',
          border: '1px solid',
          borderColor: 'whiteAlpha.200',
          rounded: 'xl',
          transition: 'all 0.2s ease-out',
          _hover: { borderColor: 'whiteAlpha.300', boxShadow: 'xl', transform: 'translateY(-4px)' },
        },
      },
    },
  },
})



