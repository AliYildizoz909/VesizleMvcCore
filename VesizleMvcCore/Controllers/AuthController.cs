﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using VesizleMvcCore.Constants;
using VesizleMvcCore.Extensions;
using VesizleMvcCore.Helpers;
using VesizleMvcCore.Identity;
using VesizleMvcCore.Models;
using VesizleMvcCore.NodejsApi.Api.Abstract;

namespace VesizleMvcCore.Controllers
{
    public class AuthController : Controller
    {
        private UserManager<VesizleUser> _userManager;
        private SignInManager<VesizleUser> _signInManager;
        private IPasswordHasher<VesizleUser> _passwordHasher;
        private IPasswordValidator<VesizleUser> _passwordValidator;
        private IUserValidator<VesizleUser> _userValidator;
        private RoleManager<VesizleRole> _roleManager;
        private IMapper _mapper;
        public AuthController(UserManager<VesizleUser> userManager, IMapper mapper, IPasswordValidator<VesizleUser> passwordValidator, IPasswordHasher<VesizleUser> passwordHasher, SignInManager<VesizleUser> signInManager, IUserValidator<VesizleUser> userValidator, RoleManager<VesizleRole> roleManager)
        {
            _userManager = userManager;
            _mapper = mapper;
            _passwordValidator = passwordValidator;
            _passwordHasher = passwordHasher;
            _signInManager = signInManager;
            _userValidator = userValidator;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<ActionResult> Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                VesizleUser user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    await _signInManager.SignOutAsync();
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    if (result.Succeeded)
                    {
                      
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError(nameof(model.Email), Messages.LoginFailed);
                return View(model);
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _mapper.Map<VesizleUser>(model);
                var identityResult = await _passwordValidator.ValidateAsync(_userManager, user, model.Password);
                if (identityResult.Succeeded)
                {
                    var validateResult = await _userValidator.ValidateAsync(_userManager, user);
                    if (validateResult.Succeeded)
                    {
                        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                        var createResult = await _userManager.CreateAsync(user);
                        var roleAddResult = await _userManager.AddToRoleAsync(user, UserRoleNames.Standard);
                        if (createResult.Succeeded && roleAddResult.Succeeded)
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        ModelState.AddIdentityError(createResult.Errors);
                        ModelState.AddIdentityError(roleAddResult.Errors);
                        return View(model);
                    }
                    ModelState.AddIdentityError(validateResult.Errors);
                    return View(model);
                }
                ModelState.AddIdentityError(nameof(model.Password), identityResult.Errors);
                return View(model);
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                VesizleUser user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    EmailHelper emailHelper = new EmailHelper();
                    var result = emailHelper.SendEmailResetPassword(model.Email, user.Id, resetToken);
                    if (result.IsSuccessful)
                    {
                        ViewBag.EmailSent = Messages.EmailSent;
                        return View(model);
                    }
                    ModelState.AddModelError(nameof(model.Email), result.Message);
                    return View(model);
                }
                ModelState.AddModelError(nameof(model.Email), Messages.EmailNotFound);
                return View(model);
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpGet("[action]/{userId}/{token}")]
        public IActionResult ResetPassword(string userId, string token)
        {
            return View();
        }
        [HttpPost("[action]/{userId}/{token}")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, string userId, string token)
        {
            if (ModelState.IsValid)
            {
                VesizleUser user = await _userManager.FindByIdAsync(userId);
                var codeDecodedBytes = WebEncoders.Base64UrlDecode(token);
                var codeDecoded = Encoding.UTF8.GetString(codeDecodedBytes);
                IdentityResult result = await _userManager.ResetPasswordAsync(user, codeDecoded, model.Password);
                if (result.Succeeded)
                {
                    var result2 = await _userManager.UpdateSecurityStampAsync(user);
                    if (result2.Succeeded)
                    {
                        return RedirectToAction("Login");
                    }
                    ModelState.AddIdentityError(result2.Errors);
                    return View(model);
                }
                ModelState.AddIdentityError(nameof(model.RePassword), result.Errors);
                return View(model);
            }
            return View(model);
        }
    }
}
