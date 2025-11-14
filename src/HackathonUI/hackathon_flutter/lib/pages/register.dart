import 'package:flutter/material.dart';
import 'package:hackathon_flutter/theme/colors.dart';
import 'package:go_router/go_router.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class LoginTextStyle {
  static const TextStyle header = TextStyle(
    fontSize: 28,
    fontWeight: FontWeight.bold,
  );
}

class _RegisterScreenState extends State<RegisterScreen> {
  final TextEditingController emailController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();
  final TextEditingController usernameController = TextEditingController();
  final TextEditingController passwordConfirmationController =
      TextEditingController();

  @override
  void dispose() {
    emailController.dispose();
    passwordController.dispose();
    super.dispose();
  }

  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  bool _submitted = false;
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Goldman Sachs')),
      body: Row(
        children: [
          Expanded(
            flex: 1,
            child: Container(
              height: double.infinity, // pełna wysokość ekranu
              child: Image.asset(
                'assets/images/hackaton2.jpg',
                fit: BoxFit
                    .cover, // zachowuje proporcje, przycina poziomo lub pionowo
              ),
            ),
          ),
          Expanded(
            flex: 1, // dokładnie 50%
            child: LayoutBuilder(
              builder: (context, constraints) {
                final bodyHeight = constraints.maxHeight;

                return SingleChildScrollView(
                  child: ConstrainedBox(
                    constraints: BoxConstraints(minHeight: bodyHeight),
                    child: IntrinsicHeight(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SizedBox(height: bodyHeight * 0.02),

                          Center(
                            child: Padding(
                              padding: const EdgeInsets.symmetric(horizontal: 16),
                              child: Text(
                                'Witaj w hackathonach Goldman Sachs!',
                                style: Theme.of(context).textTheme.headlineLarge,
                                textAlign: TextAlign.center,
                              ),
                            ),
                          ),

                          const Spacer(),

                          Align(
                            alignment: Alignment.center,
                            child: Container(
                              width: 400,
                              padding: const EdgeInsets.all(32),
                              decoration: BoxDecoration(
                                color: AppColors.background,
                                borderRadius: BorderRadius.circular(12),
                                boxShadow: [
                                  BoxShadow(
                                    color: AppColors.shadows,
                                    blurRadius: 16,
                                    offset: const Offset(0, 8),
                                  ),
                                ],
                              ),
                              child: Form(
                                key: _formKey,
                                autovalidateMode: _submitted
                                    ? AutovalidateMode.always
                                    : AutovalidateMode.disabled,
                                child: Column(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    const Text(
                                      'Rejestracja',
                                      style: LoginTextStyle.header,
                                    ),
                                    const SizedBox(height: 24),
                                    TextFormField(
                                      controller: usernameController,
                                      decoration: const InputDecoration(
                                        labelText: 'Nazwa użytkownika',
                                        border: OutlineInputBorder(),
                                      ),
                                      validator: (value) {
                                        if (value == null ||
                                            value.trim().isEmpty) {
                                          return 'Nazwa użytkownika jest wymagana';
                                        }
                                        return null;
                                      },
                                    ),
                                    SizedBox(height: 16),
                                    TextFormField(
                                      controller: emailController,
                                      decoration: const InputDecoration(
                                        labelText: 'Email',
                                        border: OutlineInputBorder(),
                                      ),
                                      validator: (value) {
                                        if (value == null ||
                                            value.trim().isEmpty) {
                                          return 'Adres email jest wymagany';
                                        }
                                        return null;
                                      },
                                    ),
                                    const SizedBox(height: 16),
                                    TextFormField(
                                      controller: passwordController,
                                      obscureText: true,
                                      decoration: const InputDecoration(
                                        labelText: 'Hasło',
                                        border: OutlineInputBorder(),
                                      ),
                                      validator: (value) {
                                        if (value == null ||
                                            value.trim().isEmpty) {
                                          return 'Hasło jest wymagane';
                                        }
                                        return null;
                                      },
                                    ),
                                    const SizedBox(height: 16),
                                    TextFormField(
                                      controller:
                                          passwordConfirmationController,
                                      obscureText: true,
                                      decoration: const InputDecoration(
                                        labelText: 'Powtórz hasło',
                                        border: OutlineInputBorder(),
                                      ),
                                      validator: (value) {
                                        if (value == null ||
                                            value.trim().isEmpty) {
                                          return 'Hasło jest wymagane';
                                        }
                                        if (value != passwordController.text) {
                                          return 'Hasła nie są takie same';
                                        }
                                        return null;
                                      },
                                    ),
                                    const SizedBox(height: 24),
                                    SizedBox(
                                      width: double.infinity,
                                      child: ElevatedButton(
                                        style: ElevatedButton.styleFrom(
                                          backgroundColor: AppColors.primary,
                                        ),
                                        onPressed: () {
                                          setState(() {
                                            _submitted = true;
                                          });
                                          if (_formKey.currentState
                                                  ?.validate() ==
                                              true) {
                                            final username = usernameController
                                                .text
                                                .trim();
                                            final email = emailController.text
                                                .trim();
                                            final password =
                                                passwordController.text;
                                            final passwordConfirmation =
                                                passwordConfirmationController
                                                    .text;
                                            print(
                                              'Nazwa użytkownika: $username, Hasło: $password, Email: $email, Powtórzone hasło: $passwordConfirmation',
                                            );
                                          }
                                        },
                                        child: const Text(
                                          'Rejestruj',
                                          style: TextStyle(
                                            color: AppColors.background,
                                          ),
                                        ),
                                      ),
                                    ),
                                    const SizedBox(height: 12),
                                    TextButton(
                                      style:
                                          TextButton.styleFrom(
                                            padding: EdgeInsets.zero,
                                            minimumSize: const Size(0, 0),
                                            tapTargetSize: MaterialTapTargetSize
                                                .shrinkWrap,
                                            foregroundColor: AppColors.primary,
                                            splashFactory:
                                                NoSplash.splashFactory,
                                          ).copyWith(
                                            overlayColor:
                                                WidgetStateProperty.all(
                                                  Colors.transparent,
                                                ),
                                          ),
                                      onPressed: () {
                                        GoRouter.of(context).go('/');
                                      },
                                      child: const Text(
                                        'Mam już konto',
                                        style: TextStyle(
                                          decoration: TextDecoration.underline,
                                          fontSize: 14,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),

                          const Spacer(),
                        ],
                      ),
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
      backgroundColor: AppColors.background,
    );
  }
}
